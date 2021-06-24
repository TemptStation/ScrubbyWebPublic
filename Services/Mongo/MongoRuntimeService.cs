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
    public class MongoRuntimeService : IRuntimeService
    {
        private readonly IMongoCollection<Round> _rounds;
        private readonly IMongoCollection<Runtime> _runtimes;

        public MongoRuntimeService(MongoAccess client)
        {
            _runtimes = client.DB.GetCollection<Runtime>("runtimes");
            _rounds = client.DB.GetCollection<Round>("rounds");
        }

        public async Task<IEnumerable<ImprovedRuntime>> GetRuntimesForRound(int roundID)
        {
            return (await _runtimes.Find(x => x.Round == roundID).ToListAsync())
                .Select(ImprovedRuntime.FromRuntime);
        }
    }
}