using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;
using ScrubbyWeb.Services.Mongo;

namespace ScrubbyWeb.Controllers
{
    public class NewscasterController : Controller
    {
        private readonly IMongoCollection<NewsCasterChannel> _News;
        private readonly IMongoCollection<Round> _Rounds;
        private readonly IMongoCollection<NewsCasterWanted> _Wanted;

        public NewscasterController(MongoAccess mongo)
        {
            _Rounds = mongo.DB.GetCollection<Round>("rounds");
            _News = mongo.DB.GetCollection<NewsCasterChannel>("newscasterchannels");
            _Wanted = mongo.DB.GetCollection<NewsCasterWanted>("newscasterwanted");
        }

        [HttpGet("round/{roundID}/newscaster")]
        public async Task<IActionResult> GetRound(int roundID)
        {
            if (await _Rounds.Find(x => x.ID == roundID).FirstOrDefaultAsync() == null) return NotFound();

            var model = new NewsCasterModel
            {
                Channels = await _News.Find(x => x.Round == roundID).ToListAsync(),
                Wanted = await _Wanted.Find(x => x.Round == roundID).ToListAsync(),
                Round = await _Rounds.Find(x => x.ID == roundID).FirstAsync()
            };

            return View(model);
        }
    }
}