using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using ScrubbyCommon.Data;
using ScrubbyWeb.Services;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ScrubbyWeb.Controllers
{
    [ApiController]
    [Route("api/population")]
    public class ServerConnectionController : Controller
    {
        private readonly IMongoCollection<ServerConnection> _connections;

        public ServerConnectionController(MongoAccess mongo)
        {
            _connections = mongo.DB.GetCollection<ServerConnection>("connections");
        }

        [HttpPost("round")]
        public async Task<IActionResult> GetPopulationForRound(JObject data)
        {
            if (!data.TryGetValue("round", out var round)) return NotFound("Round ID not found.");
            if (!data.TryGetValue("interval", out var interval)) return NotFound("Interval not found.");

            var population = new Dictionary<DateTime, int>();
            var events = new Dictionary<DateTime, int>();

            var connections = await _connections.Find(x => x.RoundID == (int) round).ToListAsync();

            foreach (var connection in connections)
            {
                if (events.ContainsKey(connection.ConnectTime))
                    events[connection.ConnectTime]++;
                else
                    events.Add(connection.ConnectTime, 1);

                if (events.ContainsKey(connection.DisconnectTime))
                    events[connection.DisconnectTime]--;
                else
                    events.Add(connection.DisconnectTime, -1);
            }

            var pop = 0;
            foreach (var timestep in events.OrderBy(x => x.Key))
            {
                pop += timestep.Value;
                population[timestep.Key] = pop;
            }

            var popOrdered = population.OrderBy(x => x.Key);

            if (!popOrdered.Any()) return Json(popOrdered);

            var startTime = popOrdered.First().Key.AddSeconds(30); // add padding to avoid the zeroes
            var endTime = popOrdered.Last().Key;
            var smoothedPopulation = new Dictionary<DateTime, int>();

            for (var time = startTime; time <= endTime; time = time.AddSeconds((int) interval))
                smoothedPopulation[time] = popOrdered.Last(x => x.Key <= time).Value;

            return Json(smoothedPopulation.ToList());
        }
    }
}