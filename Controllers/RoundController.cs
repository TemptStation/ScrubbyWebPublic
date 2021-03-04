using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;
using ScrubbyWeb.Services;

namespace ScrubbyWeb.Controllers
{
    public class RoundController : Controller
    {
        private readonly IMongoCollection<ServerConnection> _connections;
        private readonly IMongoCollection<Round> _rounds;

        public RoundController(MongoAccess mongo)
        {
            _rounds = mongo.DB.GetCollection<Round>("rounds");
            _connections = mongo.DB.GetCollection<ServerConnection>("connections");
        }

        private async Task<Round> Get(int id)
        {
            return await _rounds.Find(round => round.ID == id).FirstOrDefaultAsync();
        }

        private async Task<int> GetNext(int id, bool forward = true, List<string> ckey = null)
        {
            PipelineDefinition<Round, BsonDocument> roundSelectionPipeline = null;
            PipelineDefinition<ServerConnection, BsonDocument> roundSelectionConnectionPipeline = null;

            if (ckey != null && ckey.Any())
            {
                ckey = ckey.Distinct().ToList();
                BsonDocument ckeyFilterOr;
                if (ckey.Count != 1)
                {
                    var innerArray = new BsonArray();

                    foreach (var k in ckey) innerArray.Add(new BsonDocument().Add("CKey.Cleaned", new CKey(k).Cleaned));

                    ckeyFilterOr = new BsonDocument().Add("$or", innerArray);
                }
                else
                {
                    ckeyFilterOr = new BsonDocument().Add("CKey.Cleaned", new CKey(ckey.First()).Cleaned);
                }

                roundSelectionConnectionPipeline = new[]
                {
                    new BsonDocument("$match", ckeyFilterOr),
                    new BsonDocument("$group", new BsonDocument()
                        .Add("_id", "$RoundID")
                        .Add("Users", new BsonDocument()
                            .Add("$addToSet", "$CKey.Cleaned")
                        )),
                    new BsonDocument("$match", new BsonDocument()
                        .Add("Users", new BsonDocument()
                            .Add("$size", ckey.Count)
                        )),
                    new BsonDocument("$project", new BsonDocument()
                        .Add("_id", 1.0)),
                    new BsonDocument("$sort", new BsonDocument()
                        .Add("_id", forward ? 1 : -1)),
                    new BsonDocument("$match", new BsonDocument()
                        .Add("_id", new BsonDocument()
                            .Add(forward ? "$gt" : "$lt", id)
                        )),
                    new BsonDocument("$limit", 1)
                };
            }
            else
            {
                roundSelectionPipeline = new[]
                {
                    new BsonDocument {{"$sort", new BsonDocument("_id", forward ? 1 : -1)}},
                    new BsonDocument
                        {{"$match", new BsonDocument("_id", new BsonDocument(forward ? "$gt" : "$lt", id))}},
                    new BsonDocument {{"$limit", 1}},
                    new BsonDocument {{"$project", new BsonDocument("_id", 1)}}
                };
            }

            var result = ckey != null && ckey.Any()
                ? await _connections.Aggregate(roundSelectionConnectionPipeline).FirstOrDefaultAsync()
                : await _rounds.Aggregate(roundSelectionPipeline).FirstOrDefaultAsync();

            if (result == null) return -1;

            return (int) result["_id"];
        }

        // Redirects for original files / ned's site
        [HttpGet("round/{id}/source")]
        public async Task<IActionResult> SourceRedirect(int id)
        {
            var result = await Get(id);

            if (result == null)
                return NotFound();
            return Redirect(result.BaseURL);
        }

        [HttpGet("round/{id}/statbus")]
        public async Task<IActionResult> StatbusRedirect(int id)
        {
            var result = await Get(id);

            if (result == null)
                return NotFound();
            return Redirect($"https://sb.atlantaned.space/rounds/{id}");
        }

        [HttpGet("round/{id}")]
        public async Task<IActionResult> FetchRound(int id, [FromQuery(Name = "h")] string[] highlight)
        {
            var result = await Get(id);

            if (result == null)
            {
                var l = highlight?.ToList();
                var missingModel = new RoundModel
                {
                    CurrentRound = new Round
                    {
                        ID = id
                    },
                    NextID = await GetNext(id, true, l),
                    LastID = await GetNext(id, false, l),
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
                    NextID = await GetNext(id, true, l),
                    LastID = await GetNext(id, false, l),
                    HightlightedCkeys = l
                };
            }
            else
            {
                toGive = new RoundModel
                {
                    CurrentRound = result,
                    NextID = await GetNext(id),
                    LastID = await GetNext(id, false)
                };
            }

            var connections = await _connections.Find(x => x.RoundID == id).ToListAsync();

            var nonPlayers = new List<CKey>();
            if (connections.Count != 0 && toGive.CurrentRound.Players != null)
                nonPlayers = connections.Select(x => x.CKey)
                    .Except(toGive.CurrentRound.Players.Select(x => new CKey(x.Ckey))).ToList();

            toGive.NonPlaying = nonPlayers.OrderBy(x => x.Cleaned).ToList();

            return View("View", toGive);
        }

        [HttpGet("round")]
        public async Task<IActionResult> RoundQuery()
        {
            return View("RoundQuery");
        }

        public static string ProperName(string server)
        {
            switch (server)
            {
                case "basil":
                    return "Bagil";
                case "sybil":
                    return "Sybil";
                case "terry":
                    return "Terry";
                case "event-hall":
                    return "Event Hall";
                case "manuel":
                    return "Manuel";
                default:
                    return server;
            }
        }
    }
}