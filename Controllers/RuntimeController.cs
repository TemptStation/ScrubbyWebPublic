using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ScrubbyWeb.Models;
using ScrubbyWeb.Models.PostRequests;
using ScrubbyWeb.Services;
using ScrubbyWeb.Services.Mongo;

namespace ScrubbyWeb.Controllers
{
    public class RuntimeController : Controller
    {
        private readonly IRoundService _RoundService;
        private readonly IRuntimeService _RuntimeService;

        public RuntimeController(IRoundService roundService, IRuntimeService runtimeService)
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
    }
}