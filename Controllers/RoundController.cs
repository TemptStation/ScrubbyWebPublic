using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;
using ScrubbyWeb.Models.Data;
using ScrubbyWeb.Services.Interfaces;

namespace ScrubbyWeb.Controllers
{
    [Route("round/{id:int}")]
    public class RoundController : Controller
    {
        private readonly IRoundService _rounds;
        private readonly IConnectionService _connections;

        public RoundController(IRoundService rounds, IConnectionService connections)
        {
            _rounds = rounds;
            _connections = connections;
        }
        
        // Redirects for original files / ned's site
        [HttpGet("source")]
        public async Task<IActionResult> SourceRedirect(int id)
        {
            var result = await _rounds.GetRound(id);

            if (result == null)
                return NotFound();
            return Redirect(result.BaseURL);
        }

        [HttpGet("statbus")]
        public async Task<IActionResult> StatbusRedirect(int id)
        {
            var result = await _rounds.GetRound(id);

            if (result == null)
                return NotFound();
            return Redirect($"https://sb.atlantaned.space/rounds/{id}");
        }

        [HttpGet]
        public async Task<IActionResult> FetchRound(int id, [FromQuery(Name = "h")] string[] highlight)
        {
            var result = await _rounds.GetRound(id);

            if (result == null)
            {
                var l = highlight?.ToList();
                var missingModel = new RoundModel
                {
                    CurrentRound = new ScrubbyRound()
                    {
                        Id = id
                    },
                    NextID = await _rounds.GetNext(id, true, l),
                    LastID = await _rounds.GetNext(id, false, l),
                    HightlightedCkeys = l
                };

                return View("RoundNotFound", missingModel);
            }

            RoundModel toGive;
            if (highlight != null)
            {
                var l = highlight.ToList();
                toGive = new RoundModel
                {
                    CurrentRound = result,
                    NextID = await _rounds.GetNext(id, true, l),
                    LastID = await _rounds.GetNext(id, false, l),
                    HightlightedCkeys = l
                };
            }
            else
            {
                toGive = new RoundModel
                {
                    CurrentRound = result,
                    NextID = await _rounds.GetNext(id),
                    LastID = await _rounds.GetNext(id, false)
                };
            }

            var connections = (await _connections.GetConnectionsForRound(id)).ToList();

            var nonPlayers = new List<CKey>();
            if (connections.Count != 0 && toGive.CurrentRound.Players != null)
                nonPlayers = connections.Select(x => x.CKey)
                    .Except(toGive.CurrentRound.Players.Select(x => new CKey(x.Ckey))).ToList();

            toGive.NonPlaying = nonPlayers.OrderBy(x => x.Cleaned).ToList();

            return View("View", toGive);
        }

        public static string ProperName(string server)
        {
            return server switch
            {
                "basil" => "Bagil",
                "sybil" => "Sybil",
                "terry" => "Terry",
                "event-hall" => "Event Hall",
                "manuel" => "Manuel",
                _ => server
            };
        }
    }
}