using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;
using ScrubbyWeb.Services.Mongo;

namespace ScrubbyWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly MongoAccess _mongo;
        private readonly IMongoCollection<Round> _rounds;

        public HomeController(MongoAccess mongo)
        {
            _mongo = mongo;
            _rounds = mongo.DB.GetCollection<Round>("rounds");
        }

        [ResponseCache(Duration = 300)]
        public List<(string, DateTime, int)> GetLatest()
        {
            var toReturn = new List<(string, DateTime, int)>();

            PipelineDefinition<Round, BsonDocument> latestRoundsPipeline = new[]
            {
                new BsonDocument
                {
                    {
                        "$project", new BsonDocument().AddRange(new Dictionary<string, int>
                        {
                            {"_id", 1},
                            {"Server", 1},
                            {"Timestamp", 1}
                        })
                    }
                },
                new BsonDocument {{"$sort", new BsonDocument("_id", -1)}},
                new BsonDocument
                {
                    {
                        "$group", new BsonDocument
                        {
                            {"_id", "$Server"},
                            {"latest", new BsonDocument("$first", "$Timestamp")},
                            {"round", new BsonDocument("$first", "$_id")}
                        }
                    }
                }
            };

            var results = _rounds.Aggregate(latestRoundsPipeline).ToList();
            results.Sort((x, y) => x["_id"].ToString().CompareTo(y["_id"].ToString()));

            foreach (var result in results)
                toReturn.Add((result["_id"].ToString(), DateTime.Parse(result["latest"].ToString()),
                    int.Parse(result["round"].ToString())));

            return toReturn;
        }

        [ResponseCache(Duration = 300)]
        public (int, DateTime) GetFirst()
        {
            PipelineDefinition<Round, BsonDocument> firstRoundPipeline = new[]
            {
                new BsonDocument
                {
                    {
                        "$project", new BsonDocument().AddRange(new Dictionary<string, int>
                        {
                            {"_id", 1},
                            {"Server", 1},
                            {"Timestamp", 1}
                        })
                    }
                },
                new BsonDocument {{"$sort", new BsonDocument("_id", 1)}},
                new BsonDocument {{"$limit", 1}}
            };

            var result = _rounds.Aggregate(firstRoundPipeline).FirstOrDefault();

            if (result == null) return (-1, DateTime.MinValue);

            return (int.Parse(result["_id"].ToString()), DateTime.Parse(result["Timestamp"].ToString()));
        }

        [ResponseCache(Duration = 300)]
        public BsonDocument GetStats()
        {
            var toReturn = new BsonDocument();

            var command = new BsonDocumentCommand<BsonDocument>(new BsonDocument {{"collstats", "rounds"}});
            var stats = _mongo.DB.RunCommand(command);

            toReturn.Add("roundcount", stats["count"]);

            command = new BsonDocumentCommand<BsonDocument>(new BsonDocument {{"collstats", "rawlogs.files"}});
            stats = _mongo.DB.RunCommand(command);

            toReturn.Add("filecount", stats["count"]);

            return toReturn;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            var fr = GetFirst();
            var counts = GetStats();

            var model = new BasicStatsModel(fr.Item1, fr.Item2, GetLatest(), int.Parse(counts["roundcount"].ToString()),
                int.Parse(counts["filecount"].ToString()));

            return View(model);
        }
    }
}