using System.Threading.Tasks;
using MongoDB.Driver;
using ScrubbyCommon.Data;

namespace ScrubbyWeb.Services
{
    public class RoundService
    {
        private readonly IMongoCollection<Round> _rounds;

        public RoundService(MongoAccess client)
        {
            _rounds = client.DB.GetCollection<Round>("rounds");
        }

        public async Task<Round> GetRound(int id)
        {
            return await _rounds.Find(x => x.ID == id).FirstOrDefaultAsync();
        }
    }
}