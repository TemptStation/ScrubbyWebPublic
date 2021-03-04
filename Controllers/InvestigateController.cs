using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models.Api;
using ScrubbyWeb.Models.PostRequests;
using ScrubbyWeb.Services;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ScrubbyWeb.Controllers
{
    [Authorize(Roles = "Developer,TGAdmin,BetaTester")]
    public class InvestigateController : Controller
    {
        private readonly IMongoCollection<ServerConnection> _connections;
        private readonly PlayerService _playerService;
        private readonly IMongoCollection<Round> _rounds;

        public InvestigateController(MongoAccess mongo, PlayerService playerService)
        {
            _rounds = mongo.DB.GetCollection<Round>("rounds");
            _connections = mongo.DB.GetCollection<ServerConnection>("connections");
            _playerService = playerService;
        }

        [HttpGet("investigate")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("investigate/connections")]
        public IActionResult InvestigateConnections()
        {
            return View();
        }

        [HttpGet("investigate/suicides")]
        public IActionResult InvestigateSuicides()
        {
            return View();
        }

        [HttpPost("api/roundsforckeys")]
        public async Task<IActionResult> GetRoundsForCKeys([FromBody] RoundsForCKeyAggregationModel model)
        {
            if (model.CKeys == null || model.CKeys.Count == 0) return new StatusCodeResult(400);

            var toSearch = new List<CKey>();
            foreach (var ckey in model.CKeys) toSearch.Add(new CKey(ckey));

            var distinctCKeys = toSearch.Distinct().ToList();
            BsonDocument ckeyFilterOr;
            if (distinctCKeys.Count != 1)
            {
                var innerArray = new BsonArray();

                foreach (var k in distinctCKeys) innerArray.Add(new BsonDocument().Add("CKey.Cleaned", k.Cleaned));

                ckeyFilterOr = new BsonDocument().Add("$or", innerArray);
            }
            else
            {
                ckeyFilterOr = new BsonDocument().Add("CKey.Cleaned", distinctCKeys.First().Cleaned);
            }

            var roundsPipeline = new[]
            {
                new BsonDocument("$match", ckeyFilterOr),
                new BsonDocument("$group", new BsonDocument()
                    .Add("_id", "$RoundID")
                    .Add("Users", new BsonDocument()
                        .Add("$addToSet", "$CKey.Cleaned")
                    )),
                new BsonDocument("$match", new BsonDocument()
                    .Add("Users", new BsonDocument()
                        .Add("$size", distinctCKeys.Count)
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 1.0)),
                new BsonDocument("$sort", new BsonDocument()
                    .Add("_id", model.GTERound ? 1 : -1))
            };

            if (model.StartingRound != -1)
                roundsPipeline = roundsPipeline.Concat(new[]
                {
                    new BsonDocument("$match", new BsonDocument()
                        .Add("_id", new BsonDocument()
                            .Add(model.GTERound ? "$gte" : "$lte", model.StartingRound)
                        ))
                }).ToArray();

            if (model.Limit != -1)
                roundsPipeline = roundsPipeline.Concat(new[]
                {
                    new BsonDocument("$limit", model.Limit)
                }).ToArray();

            PipelineDefinition<ServerConnection, BsonDocument> finalPipeline = roundsPipeline;

            var result = (await _connections.Aggregate(finalPipeline).ToListAsync()).Select(x => x["_id"].AsInt32);

            PipelineDefinition<Round, BsonDocument> timePipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("_id", new BsonDocument()
                        .Add("$in", new BsonArray()
                            .AddRange(result.ToList())
                        )
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("Timestamp", 1.0)
                    .Add("Ended", 1.0))
            };

            var finalResult = (await _rounds.Aggregate(timePipeline).ToListAsync()).Select(x =>
                new
                {
                    round = x["_id"].AsInt32, started = x["Timestamp"].AsBsonDateTime, ended = x["Ended"].AsBsonDateTime
                });

            return Json(finalResult);
        }

        [HttpPost("api/connections/round")]
        public async Task<IActionResult> GetConnectionsForRound([FromBody] ConnectionsForRoundAggregationModel model)
        {
            var CKeys = new List<CKey>();
            if (model.CKeyFilter != null && model.CKeyFilter.Count > 0)
                foreach (var key in model.CKeyFilter)
                    CKeys.Add(new CKey(key));

            BsonDocument ckeyFilterOr = null;
            if (CKeys.Count != 0)
            {
                var distinctCKeys = CKeys.Distinct().ToList();

                if (distinctCKeys.Count != 1)
                {
                    var innerArray = new BsonArray();

                    foreach (var k in distinctCKeys) innerArray.Add(new BsonDocument().Add("CKey.Cleaned", k.Cleaned));

                    ckeyFilterOr = new BsonDocument().Add("$or", innerArray);
                }
                else
                {
                    ckeyFilterOr = new BsonDocument().Add("CKey.Cleaned", distinctCKeys.First().Cleaned);
                }
            }

            PipelineDefinition<ServerConnection, ServerConnection> pipeline;

            if (ckeyFilterOr != null)
                pipeline = new[]
                {
                    new BsonDocument("$match", new BsonDocument()
                        .Add("RoundID", model.Round)
                        .AddRange(ckeyFilterOr))
                };
            else
                pipeline = new[]
                {
                    new BsonDocument("$match", new BsonDocument()
                        .Add("RoundID", model.Round))
                };


            var result = await _connections.Aggregate(pipeline).ToListAsync();

            return Json(result);
        }

        [HttpPost("api/receipts")]
        public async Task<IActionResult> GetReceiptsForPlayer([FromBody] ReceiptsForPlayerPostModel model)
        {
            return Ok(await _playerService.GetRoundReceiptsForPlayer(new CKey(model.CKey), model.StartDate,
                model.EndDate));
        }
    }
}