using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Services
{
    public class ConnectionService
    {
        private readonly IMongoCollection<ServerConnection> _connections;

        public ConnectionService(MongoAccess client)
        {
            client.DB.GetCollection<Round>("rounds");
            _connections = client.DB.GetCollection<ServerConnection>("connections");
        }

        public async Task<List<ServerConnection>> GetConnectionsForCKey(CKey ckey, DateTime? startDate = null,
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

        public async Task<List<ServerConnection>> GetConnectionsForCKey(CKey ckey, int startRound, int limit)
        {
            List<ServerConnection> toReturn = null;
            toReturn = await _connections.Find(x => x.CKey.Cleaned == ckey.Cleaned && x.RoundID < startRound)
                .SortByDescending(x => x.RoundID).Limit(10 * limit).ToListAsync();
            return toReturn;
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
    }
}