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

namespace ScrubbyWeb.Services
{
    public class PlayerService
    {
        private readonly ConnectionService _connectionService;
        private readonly LogMessageService _logService;
        private readonly IMongoCollection<Round> _rounds;
        private readonly SuicideService _suicideService;

        public PlayerService(MongoAccess client, ConnectionService connectionService, SuicideService suicideService,
            LogMessageService logService)
        {
            _rounds = client.DB.GetCollection<Round>("rounds");
            _connectionService = connectionService;
            _suicideService = suicideService;
            _logService = logService;
        }

        public async Task<List<PlayerNameStatistic>> SearchForCKey(Regex regex)
        {
            var bsonRegex = new BsonRegularExpression(regex);

            PipelineDefinition<Round, PlayerNameStatistic> pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("Players.CleanKey", bsonRegex)),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 1.0)
                    .Add("Players", new BsonDocument()
                        .Add("$filter", new BsonDocument()
                            .Add("input", "$Players")
                            .Add("as", "player")
                            .Add("cond", new BsonDocument()
                                .Add("$regexMatch", new BsonDocument()
                                    .Add("input", "$$player.CleanKey")
                                    .Add("regex", bsonRegex.Pattern)
                                    .Add("options", bsonRegex.Options)
                                )
                            )
                        )
                    )),
                new BsonDocument("$unwind", new BsonDocument()
                    .Add("path", "$Players")),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", "$Players.CleanKey")
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

            var result = await _rounds.Aggregate(pipeline).ToListAsync();

            return result;
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
            var connections = await _connectionService.GetConnectionsForCKey(ckey, startingRound, limit);
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
            var connections = await _connectionService.GetConnectionsForCKey(ckey, startDate, endDate);
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
    }
}