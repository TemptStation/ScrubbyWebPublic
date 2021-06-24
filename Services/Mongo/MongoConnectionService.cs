using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Services.Mongo
{
    public class MongoConnectionService : IConnectionService
    {
        private readonly IMongoCollection<ServerConnection> _connections;

        public MongoConnectionService(MongoAccess client)
        {
            client.DB.GetCollection<Round>("rounds");
            _connections = client.DB.GetCollection<ServerConnection>("connections");
        }

        public async Task<List<ServerRoundStatistic>> GetConnectionStatsForCKey(CKey ckey, DateTime startDate)
        {
            var toReturn = new List<ServerRoundStatistic>();

            PipelineDefinition<ServerConnection, BsonDocument> pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("CKey.Cleaned", ckey.Cleaned)
                    .Add("ConnectTime", new BsonDocument()
                        .Add("$gte", new BsonDateTime(startDate))
                    )),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", new BsonDocument()
                        .Add("server", "$Server")
                        .Add("round", "$RoundID")
                    )),
                new BsonDocument("$lookup", new BsonDocument()
                    .Add("from", "rounds")
                    .Add("localField", "_id.round")
                    .Add("foreignField", "_id")
                    .Add("as", "round")),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 1.0)
                    .Add("round_start_time", new BsonDocument()
                        .Add("$arrayElemAt", new BsonArray()
                            .Add("$round.Timestamp")
                            .Add(0.0)
                        )
                    )),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", new BsonDocument()
                        .Add("server", "$_id.server")
                        .Add("day", new BsonDocument()
                            .Add("$dateToString", new BsonDocument()
                                .Add("format", "%Y-%m-%d")
                                .Add("date", "$round_start_time")
                            )
                        )
                    )
                    .Add("count", new BsonDocument()
                        .Add("$sum", 1.0)
                    ))
            };

            using (var cursor = await _connections.AggregateAsync(pipeline))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                        toReturn.Add(new ServerRoundStatistic
                        {
                            Count = document["count"].ToInt32(),
                            Date = DateTime.SpecifyKind(DateTime.Parse(document["_id"].AsBsonDocument["day"].AsString),
                                DateTimeKind.Utc),
                            Server = document["_id"].AsBsonDocument["server"].AsString
                        });
                }
            }

            return toReturn;
        }

        public async Task<List<ServerConnection>> GetConnectionsForRound(int round, IEnumerable<string> ckeys)
        {
            var ckeysList = ckeys.ToList();
            var parsedCkeys = new List<CKey>();
            if (ckeysList is {Count: > 0}) 
                parsedCkeys.AddRange(ckeysList.Select(key => new CKey(key)));

            BsonDocument ckeyFilterOr = null;
            if (parsedCkeys.Count != 0)
            {
                var distinctCKeys = parsedCkeys.Distinct().ToList();

                if (distinctCKeys.Count != 1)
                {
                    var innerArray = new BsonArray();

                    foreach (var k in distinctCKeys) innerArray.Add(new BsonDocument().Add("CKey.Cleaned", k.Cleaned));

                    ckeyFilterOr = new BsonDocument().Add("$or", innerArray);
                }
                else
                {
                    ckeyFilterOr = new BsonDocument().Add("CKey.Cleaned", distinctCKeys.First().Cleaned);
                }
            }

            PipelineDefinition<ServerConnection, ServerConnection> pipeline;

            if (ckeyFilterOr != null)
                pipeline = new[]
                {
                    new BsonDocument("$match", new BsonDocument()
                        .Add("RoundID", round)
                        .AddRange(ckeyFilterOr))
                };
            else
                pipeline = new[]
                {
                    new BsonDocument("$match", new BsonDocument()
                        .Add("RoundID", round))
                };


            return await _connections.Aggregate(pipeline).ToListAsync();
        }
    }
}