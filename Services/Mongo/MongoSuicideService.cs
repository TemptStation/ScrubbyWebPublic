using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyCommon.Data.Events;
using ScrubbyWeb.Services.Interfaces;

namespace ScrubbyWeb.Services.Mongo
{
    public class MongoSuicideService : ISuicideService
    {
        private readonly IMongoCollection<Suicide> _suicides;

        public MongoSuicideService(MongoAccess client)
        {
            _suicides = client.DB.GetCollection<Suicide>("suicides");
        }

        public async Task<List<Suicide>> GetSuicidesForCKey(CKey ckey, DateTime? startDate = null,
            DateTime? endDate = null)
        {
            List<Suicide> suicides;
            if (startDate.HasValue && endDate.HasValue)
                suicides = await _suicides.Find(x =>
                        x.CKey.Cleaned == ckey.Cleaned && x.Timestamp >= startDate.Value &&
                        x.Timestamp <= endDate.Value)
                    .ToListAsync();
            else if (startDate.HasValue)
                suicides = await _suicides.Find(x => x.CKey.Cleaned == ckey.Cleaned && x.Timestamp >= startDate.Value)
                    .ToListAsync();
            else if (endDate.HasValue)
                suicides = await _suicides.Find(x => x.CKey.Cleaned == ckey.Cleaned && x.Timestamp <= endDate.Value)
                    .ToListAsync();
            else
                suicides = await _suicides.Find(x => x.CKey.Cleaned == ckey.Cleaned).ToListAsync();

            return suicides;
        }
    }
}