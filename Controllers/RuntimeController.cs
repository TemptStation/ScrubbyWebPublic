using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ScrubbyWeb.Models;
using ScrubbyWeb.Models.PostRequests;
using ScrubbyWeb.Services;

namespace ScrubbyWeb.Controllers
{
    public class RuntimeController : Controller
    {
        private readonly RoundService _RoundService;
        private readonly RuntimeService _RuntimeService;

        public RuntimeController(RoundService roundService, RuntimeService runtimeService)
        {
            _RoundService = roundService;
            _RuntimeService = runtimeService;
        }

        [HttpGet("round/{roundID}/runtimes")]
        public async Task<IActionResult> GetRound(int roundID)
        {
            var round = await _RoundService.GetRound(roundID);
            var runtimes = await _RuntimeService.GetRuntimesForRound(roundID);
            return View(new RoundRuntimeModel
            {
                RoundID = round.ID,
                Runtimes = runtimes.ToList(),
                Version = round.VersionInfo
            });
        }

        [HttpPost("api/da/runtimes/pr")]
        public async Task<IActionResult> GetRuntimesForPR([FromBody] RuntimeSearchPostModel<int> model)
        {
            var result = await _RuntimeService.GetRuntimesForPR(model.Data, model.StartDate, model.EndDate);
            return Ok(result);
        }

        [HttpPost("api/da/runtimes/commit")]
        public async Task<IActionResult> GetRuntimesForPR([FromBody] RuntimeSearchPostModel<string> model)
        {
            var result = await _RuntimeService.GetRuntimesForCommit(model.Data, model.StartDate, model.EndDate);
            return Ok(result);
        }
    }
}