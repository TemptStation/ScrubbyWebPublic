using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Services
{
    public class AnnouncementService
    {
        private readonly IMongoCollection<ScrubbyAnnouncement> _announcements;

        public AnnouncementService(MongoAccess client)
        {
            _announcements = client.DB.GetCollection<ScrubbyAnnouncement>("scrubby_announcements");
        }

        public async Task<List<ScrubbyAnnouncement>> GetAnnouncements()
        {
            var now = DateTime.UtcNow;
            return await _announcements.Find(x => x.Active < now && x.Expires > now).ToListAsync();
        }
    }
}