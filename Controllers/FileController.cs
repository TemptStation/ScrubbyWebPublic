using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;
using ScrubbyWeb.Models.PostRequests;
using ScrubbyWeb.Services.Interfaces;
using ScrubbyWeb.Services.Mongo;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ScrubbyWeb.Controllers
{
    public class FileController : Controller
    {
        private static readonly Regex _validRange = new Regex(@"^(?<lower>[0-9]+)-(?<upper>[0-9]+)$");
        private readonly IMongoCollection<LogMessage> _messages;
        private readonly IRoundService _rounds;

        public FileController(MongoAccess mongo, IRoundService rounds)
        {
            _rounds = rounds;
            _messages = mongo.DB.GetCollection<LogMessage>("logmessages");
        }

        [HttpGet("round/{roundid}/files")]
        public async Task<IActionResult> FetchFile(int roundid, [FromQuery(Name = "file")] string[] files,
            [FromQuery(Name = "ckey")] string[] ckeys, [FromQuery(Name = "range")] string[] ranges)
        {
            var round = await _rounds.GetRound(roundid);
            if (round == null || files == null || files.Length == 0) return NotFound();
            files = files.Select(x => x.ToLower()).Where(x => round.Files.Any(y => y.FileName == x)).Distinct()
                .ToArray();

            var link = $"https://scrubby.melonmesa.com/round/{round.ID}/files?";
            foreach (var file in files) link = $"{link}file={file}&";
            if (ckeys != null && ckeys.Length > 0)
                foreach (var ckey in ckeys)
                    link = $"{link}ckey={ckey}&";
            if (ranges != null && ranges.Length > 0)
                foreach (var range in ranges)
                    link = $"{link}range={range}&";

            var model = new LogModel
            {
                Parent = round,
                Data = new FileMessagePostModel
                {
                    RoundID = roundid,
                    CKeys = ckeys,
                    Files = files,
                    Ranges = ranges
                }
            };

            return View("View", model);
        }

        [HttpPost("api/file/messages")]
        public async Task<IActionResult> FetchMessages([FromBody] FileMessagePostModel model)
        {
            var ignoreRanges = model.Ranges == null || model.Ranges.Length == 0;
            var round = await _rounds.GetRound(model.RoundID);

            if (round == null || model.Files == null || model.Files.Length == 0) return Ok(null);

            if (!ignoreRanges && model.Files != null && model.Files.Length == 1)
                foreach (var r in model.Ranges)
                    if (!_validRange.IsMatch(r))
                    {
                        ignoreRanges = true;
                        break;
                    }

            var matchingFiles = round.Files.Where(x => model.Files.Contains(x.FileName)).Select(x => x._id);
            var aggregation = _messages.Aggregate();
            aggregation = aggregation.Match(x => matchingFiles.Contains(x.ParentFile));

            if (model.CKeys != null && model.CKeys.Length != 0)
            {
                var ckeyindividual = new List<FilterDefinition<LogMessage>>();
                foreach (var k in model.CKeys)
                    ckeyindividual.Add(new FilterDefinitionBuilder<LogMessage>().Regex("Message",
                        new BsonRegularExpression($"{k}", "i")));
                var ckeyfilter = new FilterDefinitionBuilder<LogMessage>().Or(ckeyindividual);
                aggregation = aggregation.Match(ckeyfilter);
            }

            if (!ignoreRanges)
            {
                var rangesIndividual = new List<FilterDefinition<LogMessage>>();
                foreach (var r in model.Ranges)
                {
                    var match = _validRange.Match(r);
                    var lower = int.Parse(match.Groups["lower"].Value);
                    var upper = int.Parse(match.Groups["upper"].Value);
                    var lowerDef = new FilterDefinitionBuilder<LogMessage>().Lte(x => x.RelativeIndex, upper);
                    var upperDef = new FilterDefinitionBuilder<LogMessage>().Gte(x => x.RelativeIndex, lower);
                    rangesIndividual.Add(new FilterDefinitionBuilder<LogMessage>().And(lowerDef, upperDef));
                }

                var rangefilter = new FilterDefinitionBuilder<LogMessage>().Or(rangesIndividual);
                aggregation = aggregation.Match(rangefilter);
            }

            var sortdef = new SortDefinitionBuilder<LogMessage>().Ascending("Timestamp");
            if (matchingFiles.Count() == 1)
                sortdef = new SortDefinitionBuilder<LogMessage>().Ascending("RelativeIndex");
            aggregation = aggregation.Sort(sortdef);
            var messages = aggregation.ToList();

            return Ok(messages);
        }
    }
}