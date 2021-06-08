using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;

namespace ScrubbyWeb.Services.Mongo
{
    public class MongoRuntimeService : IRuntimeService
    {
        private readonly IMongoCollection<Round> _rounds;
        private readonly IMongoCollection<Runtime> _runtimes;

        public MongoRuntimeService(MongoAccess client)
        {
            _runtimes = client.DB.GetCollection<Runtime>("runtimes");
            _rounds = client.DB.GetCollection<Round>("rounds");
        }

        public async Task<IEnumerable<Runtime>> GetRuntimesForRound(int roundID)
        {
            return await _runtimes.Find(x => x.Round == roundID).ToListAsync();
        }

        public async Task<IEnumerable<Runtime>> GetRuntimesForCommit(string commitID, DateTime startDate,
            DateTime endDate)
        {
            PipelineDefinition<Round, Round> roundPipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("VersionInfo.Master", commitID)
                    .Add("Timestamp", new BsonDocument()
                        .Add("$gte", new BsonDateTime(startDate))
                        .Add("$lte", new BsonDateTime(endDate))
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 1.0))
            };
            var applicableRounds = (await _rounds.Aggregate(roundPipeline).ToListAsync()).Select(x => x.ID);
            return await _runtimes.Find(x => applicableRounds.Contains(x.Round)).ToListAsync();
        }

        public async Task<IEnumerable<Runtime>> GetRuntimesForPR(int pr, DateTime startDate, DateTime endDate)
        {
            PipelineDefinition<Round, Round> roundPipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("VersionInfo.TestMerges.PR", pr)
                    .Add("Timestamp", new BsonDocument()
                        .Add("$gte", new BsonDateTime(startDate))
                        .Add("$lte", new BsonDateTime(endDate))
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 1.0))
            };
            var applicableRounds = (await _rounds.Aggregate(roundPipeline).ToListAsync()).Select(x => x.ID);
            return await _runtimes.Find(x => applicableRounds.Contains(x.Round)).ToListAsync();
        }
    }
}