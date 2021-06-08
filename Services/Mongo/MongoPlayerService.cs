using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyCommon.Data.Events;
using ScrubbyWeb.Models;

namespace ScrubbyWeb.Services.Mongo
{
    public class MongoPlayerService : IPlayerService
    {
        private readonly IConnectionService _connectionService;
        private readonly ILogMessageService _logService;
        private readonly IMongoCollection<Round> _rounds;
        private readonly IMongoCollection<ServerConnection> _connections;
        private readonly ISuicideService _suicideService;

        public MongoPlayerService(MongoAccess client, IConnectionService connectionService, ISuicideService suicideService,
            ILogMessageService logService)
        {
            _rounds = client.DB.GetCollection<Round>("rounds");
            _connections = client.DB.GetCollection<ServerConnection>("connections");
            _connectionService = connectionService;
            _suicideService = suicideService;
            _logService = logService;
        }

        public async Task<List<PlayerNameStatistic>> SearchForCKey(Regex regex)
        {
            var bsonRegex = new BsonRegularExpression(regex);

            PipelineDefinition<ServerConnection, PlayerNameStatistic> pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("CKey.Cleaned", bsonRegex)),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", new BsonDocument() {{ "ckey", "$CKey.Cleaned" }, { "round", "$RoundID" }})),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", "$_id.ckey")
                    .Add("count", new BsonDocument()
                        .Add("$sum", 1.0)
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 0.0)
                    .Add("RawCKey", "$_id")
                    .Add("Count", "$count")),
                new BsonDocument("$sort", new BsonDocument()
                    .Add("Count", -1.0))
            };
            
            return await _connections.Aggregate(pipeline).ToListAsync();;
        }

        public async Task<List<PlayerNameStatistic>> SearchForICName(Regex regex)
        {
            var bsonRegex = new BsonRegularExpression(regex);

            PipelineDefinition<Round, PlayerNameStatistic> pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("Players.Name", bsonRegex)),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 1.0)
                    .Add("Players", new BsonDocument()
                        .Add("$filter", new BsonDocument()
                            .Add("input", "$Players")
                            .Add("as", "player")
                            .Add("cond", new BsonDocument()
                                .Add("$regexMatch", new BsonDocument()
                                    .Add("input", "$$player.Name")
                                    .Add("regex", bsonRegex.Pattern)
                                    .Add("options", bsonRegex.Options)
                                )
                            )
                        )
                    )),
                new BsonDocument("$unwind", new BsonDocument()
                    .Add("path", "$Players")),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", new BsonDocument()
                        .Add("Name", "$Players.Name")
                        .Add("CKey", "$Players.CleanKey")
                    )
                    .Add("count", new BsonDocument()
                        .Add("$sum", 1.0)
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 0.0)
                    .Add("ICName", "$_id.Name")
                    .Add("RawCKey", "$_id.CKey")
                    .Add("Count", "$count")),
                new BsonDocument("$sort", new BsonDocument()
                    .Add("Count", -1.0))
            };

            var result = await _rounds.Aggregate(pipeline).ToListAsync();

            return result;
        }

        public async Task<List<RoundReceipt>> GetRoundReceiptsForPlayer(CKey ckey, int startingRound, int limit)
        {
            var connections = await GetConnectionsForCKey(ckey, startingRound, limit);
            var roundsToFind = connections.Select(x => x.RoundID).Distinct();
            PipelineDefinition<Round, Round> pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("_id", new BsonDocument()
                        .Add("$in", new BsonArray()
                            .AddRange(roundsToFind)
                        )
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("Timestamp", 1.0)
                    .Add("Server", 1.0)
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
                    ))
            };
            var rounds = (await _rounds.Aggregate(pipeline).ToListAsync()).OrderByDescending(x => x.ID).ToList();
            var suicides = await _suicideService.GetSuicidesForCKey(ckey, startingRound, limit);
            return await ProcessRoundDataForReceipts(ckey, connections, rounds, suicides);
        }

        public async Task<List<RoundReceipt>> GetRoundReceiptsForPlayer(CKey ckey, DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var connections = await GetConnectionsForCKey(ckey, startDate, endDate);
            var roundsToFind = connections.Select(x => x.RoundID).Distinct();
            PipelineDefinition<Round, Round> pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("_id", new BsonDocument()
                        .Add("$in", new BsonArray()
                            .AddRange(roundsToFind)
                        )
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("Timestamp", 1.0)
                    .Add("Server", 1.0)
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
                    ))
            };
            var rounds = await _rounds.Aggregate(pipeline).ToListAsync();
            var suicides = await _suicideService.GetSuicidesForCKey(ckey, startDate, endDate);
            return await ProcessRoundDataForReceipts(ckey, connections, rounds, suicides);
        }

        private async Task<List<RoundReceipt>> ProcessRoundDataForReceipts(CKey ckey,
            List<ServerConnection> connections, List<Round> rounds, List<Suicide> suicides)
        {
            var toReturn = new List<RoundReceipt>();
            var roundStartSuicideTime = new TimeSpan(0, 10, 0);

            foreach (var round in rounds.Where(x => x.ID >= 77512)) // 77512 is when round join time started being used
            {
                var roundConnections = connections.Where(x => x.RoundID == round.ID);
                var connectionTotal = new TimeSpan();
                foreach (var connection in roundConnections)
                    connectionTotal += connection.DisconnectTime - connection.ConnectTime;
                var playedInRound = roundConnections.First().Played;
                var wasAntag = playedInRound && round.Players.Any(x => x.CleanKey == ckey.Cleaned && x.Role != "none");
                var wasRoundStart = playedInRound &&
                                    round.Players.Any(x => x.CleanKey == ckey.Cleaned && x.Jointime == "roundstart");

                var roundSuicides = suicides.Where(x => x.RoundID == round.ID);
                Suicide firstSuicide = null;
                LogMessage firstSuicideEvidence = null;
                if (roundSuicides.Any())
                {
                    firstSuicide = roundSuicides.OrderBy(x => x.RelativeTime).First();

                    if (firstSuicide.RelativeTime <= roundStartSuicideTime)
                        firstSuicideEvidence = await _logService.GetMessage(firstSuicide.Evidence);
                }

                var toInsert = new RoundReceipt
                {
                    RoundID = round.ID,
                    Timestamp = round.Timestamp,
                    ConnectedTime = connectionTotal,
                    PlayedInRound = playedInRound,
                    Antagonist = wasAntag,
                    RoundStartPlayer = wasRoundStart,
                    RoundStartSuicide = firstSuicide != null && firstSuicide.RelativeTime <= roundStartSuicideTime,
                    FirstSuicide = firstSuicide,
                    FirstSuicideEvidence = firstSuicideEvidence,
                    Server = round.Server
                };

                if (playedInRound)
                {
                    toInsert.Job = round.Players.First(x => x.CleanKey == ckey.Cleaned).Job;
                    toInsert.Name = round.Players.First(x => x.CleanKey == ckey.Cleaned).Name;
                }

                toReturn.Add(toInsert);
            }

            return toReturn;
        }
        
        private async Task<List<ServerConnection>> GetConnectionsForCKey(CKey ckey, DateTime? startDate = null,
            DateTime? endDate = null)
        {
            List<ServerConnection> toReturn = null;

            if (startDate.HasValue && endDate.HasValue)
                toReturn = await _connections.Find(x =>
                    x.CKey.Cleaned == ckey.Cleaned && x.ConnectTime >= startDate.Value &&
                    x.ConnectTime <= endDate.Value).ToListAsync();
            else if (startDate.HasValue)
                toReturn = await _connections
                    .Find(x => x.CKey.Cleaned == ckey.Cleaned && x.ConnectTime >= startDate.Value).ToListAsync();
            else if (endDate.HasValue)
                toReturn = await _connections
                    .Find(x => x.CKey.Cleaned == ckey.Cleaned && x.ConnectTime <= endDate.Value).ToListAsync();
            else
                toReturn = await _connections.Find(x => x.CKey.Cleaned == ckey.Cleaned).ToListAsync();

            return toReturn;
        }

        private async Task<List<ServerConnection>> GetConnectionsForCKey(CKey ckey, int startRound, int limit)
        {
            List<ServerConnection> toReturn = null;
            toReturn = await _connections.Find(x => x.CKey.Cleaned == ckey.Cleaned && x.RoundID < startRound)
                .SortByDescending(x => x.RoundID).Limit(10 * limit).ToListAsync();
            return toReturn;
        }
    }
}