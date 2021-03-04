using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;

namespace ScrubbyWeb.Services
{
    public class LogMessageService
    {
        private readonly IMongoCollection<LogMessage> _messages;

        public LogMessageService(MongoAccess client)
        {
            client.DB.GetCollection<Round>("rounds");
            _messages = client.DB.GetCollection<LogMessage>("logmessages");
        }

        public async Task<LogMessage> GetMessage(ObjectId id)
        {
            return await _messages.Find(x => x.MessageID == id).FirstOrDefaultAsync();
        }

        public async Task<List<LogMessage>> GetMessages(ObjectId file, FilterDefinition<LogMessage> filter = null)
        {
            var toReturn = _messages.Find(x => x.ParentFile == file);
            if (filter != null) toReturn.Filter &= filter;
            return await toReturn.ToListAsync();
        }

        public async Task<List<LogMessage>> GetMessages(IEnumerable<ObjectId> files,
            FilterDefinition<LogMessage> filter = null)
        {
            var toReturn = _messages.Find(x => files.Contains(x.ParentFile));
            if (filter != null) toReturn.Filter &= filter;
            return await toReturn.ToListAsync();
        }
    }
}