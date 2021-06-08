using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;

namespace ScrubbyWeb.Services
{
    public interface ILogMessageService
    {
        Task<LogMessage> GetMessage(ObjectId id);
        Task<List<LogMessage>> GetMessages(ObjectId file, FilterDefinition<LogMessage> filter = null);

        Task<List<LogMessage>> GetMessages(IEnumerable<ObjectId> files,
            FilterDefinition<LogMessage> filter = null);
    }
}