using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models.CommonRounds;
using ScrubbyWeb.Services.Interfaces;

namespace ScrubbyWeb.Services.Mongo
{
    public class MongoRoundService : IRoundService
    {
        private readonly IMongoCollection<ServerConnection> _connections;
        private readonly IMongoCollection<Round> _rounds;

        public MongoRoundService(MongoAccess client)
        {
            _rounds = client.DB.GetCollection<Round>("rounds");
            _connections = client.DB.GetCollection<ServerConnection>("connections");
        }

        public async Task<Round> GetRound(int id)
        {
            return await _rounds.Find(x => x.ID == id).FirstOrDefaultAsync();
        }

        public async Task<List<CommonRoundModel>> GetCommonRounds(IEnumerable<string> ckeys,
            CommonRoundsOptions options = null)
        {
            var toSearch = new List<CKey>();
            foreach (var ckey in ckeys) toSearch.Add(new CKey(ckey));

            var distinctCKeys = toSearch.Distinct().ToList();
            BsonDocument ckeyFilterOr;
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

            var roundsPipeline = new[]
            {
                new BsonDocument("$match", ckeyFilterOr),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", "$RoundID")
                    .Add("Users", new BsonDocument()
                        .Add("$addToSet", "$CKey.Cleaned")
                    )),
                new BsonDocument("$match", new BsonDocument()
                    .Add("Users", new BsonDocument()
                        .Add("$size", distinctCKeys.Count)
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 1.0)),
                new BsonDocument("$sort", new BsonDocument()
                    .Add("_id", options == null ? 1 : options.GTERound ? 1 : -1))
            };

            if (options?.StartingRound != null)
                roundsPipeline = roundsPipeline.Concat(new[]
                {
                    new BsonDocument("$match", new BsonDocument()
                        .Add("_id", new BsonDocument()
                            .Add(options.GTERound ? "$gte" : "$lte", options.StartingRound ?? -1)
                        ))
                }).ToArray();

            if (options?.Limit != null)
                roundsPipeline = roundsPipeline.Concat(new[]
                {
                    new BsonDocument("$limit", options.Limit)
                }).ToArray();

            PipelineDefinition<ServerConnection, BsonDocument> finalPipeline = roundsPipeline;

            var result = (await _connections.Aggregate(finalPipeline).ToListAsync()).Select(x => x["_id"].AsInt32);

            PipelineDefinition<Round, BsonDocument> timePipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("_id", new BsonDocument()
                        .Add("$in", new BsonArray()
                            .AddRange(result.ToList())
                        )
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("Timestamp", 1.0)
                    .Add("Ended", 1.0))
            };

            return (await _rounds.Aggregate(timePipeline).ToListAsync()).Select(x =>
                new CommonRoundModel
                {
                    Round = x["_id"].AsInt32, Started = x["Timestamp"].ToNullableUniversalTime(),
                    Ended = x["Ended"].ToNullableUniversalTime()
                }).ToList();
        }
    }
}