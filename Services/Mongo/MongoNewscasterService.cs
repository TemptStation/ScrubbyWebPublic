using System.Threading.Tasks;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;
using ScrubbyWeb.Services.Interfaces;

namespace ScrubbyWeb.Services.Mongo
{
    public class MongoNewscasterService : INewscasterService
    {
        private readonly IMongoCollection<NewsCasterChannel> _News;
        private readonly IMongoCollection<Round> _Rounds;
        private readonly IMongoCollection<NewsCasterWanted> _Wanted;

        public MongoNewscasterService(MongoAccess mongo)
        {
            _Rounds = mongo.DB.GetCollection<Round>("rounds");
            _News = mongo.DB.GetCollection<NewsCasterChannel>("newscasterchannels");
            _Wanted = mongo.DB.GetCollection<NewsCasterWanted>("newscasterwanted");
        }
        
        public async Task<NewsCasterModel> GetRound(int round)
        {
            if (await _Rounds.Find(x => x.ID == round).FirstOrDefaultAsync() == null)
                return null;
            
            return new NewsCasterModel
            {
                Channels = await _News.Find(x => x.Round == round).ToListAsync(),
                Wanted = await _Wanted.Find(x => x.Round == round).ToListAsync(),
                Round = await _Rounds.Find(x => x.ID == round).FirstAsync()
            };
        }
    }
}