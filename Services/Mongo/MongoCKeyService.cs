using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Services.Mongo
{
    public class MongoCKeyService : ICKeyService
    {
        private readonly IMongoCollection<Round> _rounds;
        private readonly IMongoCollection<ServerConnection> _connections;

        public MongoCKeyService(MongoAccess mongo)
        {
            _rounds = mongo.DB.GetCollection<Round>("rounds");
            _connections = mongo.DB.GetCollection<ServerConnection>("connections");
        }

        public async Task<List<NameCountRecord>> GetNamesForCKeyAsync(CKey ckey)
        {
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

            return results.Select(result => new NameCountRecord()
                {
                    Name = result["_id"]["name"]
                        .ToString(),
                    Count = result["count"]
                        .ToInt32()
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        public async Task<List<ServerStatistic>> GetServerCountForCKeyAsync(CKey ckey)
        {
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

            return (await _connections.Aggregate(pipeline).ToListAsync())
                .OrderBy(x => x.Server)
                .ToList();
        }

        public async Task<string> GetByondKeyAsync(CKey ckey)
        {
            return null;
        }
    }
}