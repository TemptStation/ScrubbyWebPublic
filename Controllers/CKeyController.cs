using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;
using ScrubbyWeb.Models.Data;
using ScrubbyWeb.Models.PostRequests;
using ScrubbyWeb.Services;

namespace ScrubbyWeb.Controllers
{
    public class CKeyController : Controller
    {
        private readonly IMongoCollection<ServerConnection> _connections;
        private readonly ConnectionService _connService;
        private readonly PlayerService _players;
        private readonly IMongoCollection<Round> _rounds;
        private readonly SuicideService _suicides;

        public CKeyController(MongoAccess mongo, PlayerService players, SuicideService suicides,
            ConnectionService connService)
        {
            _rounds = mongo.DB.GetCollection<Round>("rounds");
            _connections = mongo.DB.GetCollection<ServerConnection>("connections");
            _players = players;
            _suicides = suicides;
            _connService = connService;
        }

        [ResponseCache(Duration = 300)]
        public async Task<List<(string, int)>> FetchNames(CKey ckey)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var toReturn = new List<(string, int)>();

            // TODO: see if filter operation is faster than unwind match
            PipelineDefinition<Round, BsonDocument> pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("Players.CleanKey", ckey.Cleaned)),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 1.0)
                    .Add("Players", new BsonDocument()
                        .Add("$filter", new BsonDocument()
                            .Add("input", "$Players")
                            .Add("as", "player")
                            .Add("cond", new BsonDocument()
                                .Add("$eq", new BsonArray()
                                    .Add("$$player.CleanKey")
                                    .Add(ckey.Cleaned)
                                )
                            )
                        )
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 1.0)
                    .Add("Players", new BsonDocument()
                        .Add("$arrayElemAt", new BsonArray()
                            .Add("$Players")
                            .Add(0.0)
                        )
                    )),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", new BsonDocument()
                        .Add("name", "$Players.Name")
                    )
                    .Add("count", new BsonDocument()
                        .Add("$sum", 1.0)
                    ))
            };

            var results = await (await _rounds.AggregateAsync(pipeline)).ToListAsync();

            foreach (var result in results) toReturn.Add((result["_id"]["name"].ToString(), result["count"].ToInt32()));

            toReturn.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            stopWatch.Stop();
            Console.WriteLine($"FetchNames({ckey.Cleaned}) finished in {stopWatch.ElapsedMilliseconds} ms");
            return toReturn;
        }

        [ResponseCache(Duration = 300)]
        public async Task<List<ServerStatistic>> FetchServerCount(CKey ckey)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            PipelineDefinition<ServerConnection, ServerStatistic> pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("CKey.Cleaned", ckey.Cleaned)),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 1.0)
                    .Add("RoundID", 1.0)
                    .Add("CKey", 1.0)
                    .Add("Server", 1.0)
                    .Add("Played", 1.0)
                    .Add("Playtime", new BsonDocument()
                        .Add("$subtract", new BsonArray()
                            .Add("$DisconnectTime")
                            .Add("$ConnectTime")
                        )
                    )),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", new BsonDocument()
                        .Add("Round", "$RoundID")
                        .Add("Server", "$Server")
                        .Add("Played", "$Played")
                    )
                    .Add("Playtime", new BsonDocument()
                        .Add("$sum", "$Playtime")
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 1.0)
                    .Add("PlayedTime", new BsonDocument()
                        .Add("$cond", new BsonArray()
                            .Add(new BsonDocument()
                                .Add("$eq", new BsonArray()
                                    .Add("$_id.Played")
                                    .Add(new BsonBoolean(true))
                                )
                            )
                            .Add("$Playtime")
                            .Add(0.0)
                        )
                    )
                    .Add("ConnectedTime", new BsonDocument()
                        .Add("$cond", new BsonArray()
                            .Add(new BsonDocument()
                                .Add("$eq", new BsonArray()
                                    .Add("$_id.Played")
                                    .Add(new BsonBoolean(false))
                                )
                            )
                            .Add("$Playtime")
                            .Add(0.0)
                        )
                    )),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", new BsonDocument()
                        .Add("Server", "$_id.Server")
                        .Add("Played", "$_id.Played")
                    )
                    .Add("Count", new BsonDocument()
                        .Add("$sum", 1.0)
                    )
                    .Add("PlayedTime", new BsonDocument()
                        .Add("$sum", "$PlayedTime")
                    )
                    .Add("ConnectedTime", new BsonDocument()
                        .Add("$sum", "$ConnectedTime")
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("PlayedTime", 1.0)
                    .Add("ConnectedTime", 1.0)
                    .Add("Played", new BsonDocument()
                        .Add("$cond", new BsonArray()
                            .Add(new BsonDocument()
                                .Add("$eq", new BsonArray()
                                    .Add("$_id.Played")
                                    .Add(new BsonBoolean(true))
                                )
                            )
                            .Add("$Count")
                            .Add(0.0)
                        )
                    )
                    .Add("Connected", new BsonDocument()
                        .Add("$cond", new BsonArray()
                            .Add(new BsonDocument()
                                .Add("$eq", new BsonArray()
                                    .Add("$_id.Played")
                                    .Add(new BsonBoolean(false))
                                )
                            )
                            .Add("$Count")
                            .Add(0.0)
                        )
                    )),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", "$_id.Server")
                    .Add("Played", new BsonDocument()
                        .Add("$sum", "$Played")
                    )
                    .Add("Connected", new BsonDocument()
                        .Add("$sum", "$Connected")
                    )
                    .Add("PlayedMillisec", new BsonDocument()
                        .Add("$sum", "$PlayedTime")
                    )
                    .Add("ConnectedMillisec", new BsonDocument()
                        .Add("$sum", "$ConnectedTime")
                    ))
            };

            var toReturn = await _connections.Aggregate(pipeline).ToListAsync();
            toReturn = toReturn.OrderBy(x => x.Server).ToList();
            stopWatch.Stop();
            Console.WriteLine($"FetchServerCount({ckey.Cleaned}) finished in {stopWatch.ElapsedMilliseconds} ms");
            return toReturn;
        }

        [ResponseCache(Duration = 300)]
        public async Task<List<int>> FetchRoundsForCkey(CKey ckey, int limit = 5)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            PipelineDefinition<ServerConnection, BsonDocument> firstPipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("CKey.Cleaned", ckey.Cleaned)),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", "$RoundID")),
                new BsonDocument("$sort", new BsonDocument()
                    .Add("_id", 1.0)),
                new BsonDocument("$limit", 1.0)
            };

            PipelineDefinition<ServerConnection, BsonDocument> recentPipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("CKey.Cleaned", ckey.Cleaned)),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", "$RoundID")),
                new BsonDocument("$sort", new BsonDocument()
                    .Add("_id", -1.0)),
                new BsonDocument("$limit", 5.0)
            };

            List<int> first = null;
            List<int> recent = null;

            var dataFetch = new Task[]
            {
                Task.Run(async () => first = (await _connections.Aggregate(firstPipeline).ToListAsync())
                    .Select(x => x["_id"].AsInt32).ToList()),
                Task.Run(async () => recent = (await _connections.Aggregate(recentPipeline).ToListAsync())
                    .Select(x => x["_id"].AsInt32).ToList())
            };

            await Task.WhenAll(dataFetch);
            first.AddRange(recent);

            stopWatch.Stop();
            Console.WriteLine(
                $"FetchRoundsForCkey({ckey.Cleaned}, {limit}) finished in {stopWatch.ElapsedMilliseconds} ms");
            return first;
        }

        [HttpGet("ckey/{ckey}")]
        public async Task<IActionResult> FetchCKey(string ckey)
        {
            var key = new CKey(ckey);
            var data = await FetchNames(key);

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var playtime = new List<ServerStatistic>();
            List<int> rounds = null;
            var dataFetch = new Task[]
            {
                Task.Run(async () => playtime = await FetchServerCount(key)),
                Task.Run(async () => rounds = await FetchRoundsForCkey(key))
            };

            await Task.WhenAll(dataFetch);
            rounds.Sort();
            stopWatch.Stop();
            Console.WriteLine($"Data fetch for {ckey} finished in {stopWatch.ElapsedMilliseconds} ms");

            if (rounds.Count == 0) return NotFound();

            var toGive = new CKeyModel
            {
                Key = new CKey(ckey),
                Names = data,
                Playtime = playtime,
                Rounds = rounds
            };

            return View("View", toGive);
        }

        [HttpGet("suicides/{ckey}")]
        public async Task<IActionResult> GetSuicidesForCKey(string ckey)
        {
            var toSearch = new CKey(ckey);
            var result = await _suicides.GetSuicidesForCKey(toSearch);
            return Ok(result);
        }

        [HttpGet("receipts/{ckey}")]
        public async Task<IActionResult> GetReceiptsForCKey(string ckey)
        {
            var toSearch = new CKey(ckey);
            var result = await _players.GetRoundReceiptsForPlayer(toSearch);
            return Ok(result);
        }

        [HttpPost("ckey/{ckey}/connections")]
        public async Task<IActionResult> GetConnectionData(string ckey, int length = 180)
        {
            if (string.IsNullOrEmpty(ckey))
            {
                return NotFound("Failed to find ckey, or invalid ckey");
            }

            var toSearch = new CKey(ckey);
            var result = await _connService.GetConnectionStatsForCKey(toSearch, DateTime.UtcNow.AddDays(-1 * Math.Abs(length)));
            return Ok(result);
        }

        [HttpPost("ckey/{ckey}/receipts")]
        public async Task<IActionResult> GetReceipts([FromBody] ReceiptRetrievalModel model)
        {
            if (string.IsNullOrEmpty(model.CKey))
            {
                return NotFound("Failed to find ckey, or invalid ckey");
            }

            var toSearch = new CKey(model.CKey);
            var result = await _players.GetRoundReceiptsForPlayer(toSearch, model.StartingRound, model.Limit);
            return Ok(result);
        }
    }
}