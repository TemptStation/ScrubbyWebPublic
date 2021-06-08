using System.Threading.Tasks;
using MongoDB.Driver;
using ScrubbyCommon.Data;

namespace ScrubbyWeb.Services.Mongo
{
    public class MongoRoundService : IRoundService
    {
        private readonly IMongoCollection<Round> _rounds;

        public MongoRoundService(MongoAccess client)
        {
            _rounds = client.DB.GetCollection<Round>("rounds");
        }

        public async Task<Round> GetRound(int id)
        {
            return await _rounds.Find(x => x.ID == id).FirstOrDefaultAsync();
        }
    }
}