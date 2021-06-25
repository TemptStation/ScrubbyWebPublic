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
        private readonly IRoundService _roundService;
        private readonly IRuntimeService _runtimeService;

        public RuntimeController(IRoundService roundService, IRuntimeService runtimeService)
        {
            _roundService = roundService;
            _runtimeService = runtimeService;
        }

        [HttpGet("round/{round:int}/runtimes")]
        public async Task<IActionResult> GetRound(int round)
        {
            var dbRound = await _roundService.GetRound(round);
            return View(new RoundRuntimeModel
            {
                RoundID = dbRound.ID,
                Version = dbRound.VersionInfo
            });
        }

        [HttpGet("api/runtime/{round:int}")]
        public async Task<IActionResult> GetRuntimesForRound(int round)
        {
            return Ok(await _runtimeService.GetRuntimesForRound(round));
        }
    }
}