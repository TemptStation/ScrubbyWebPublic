using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models;
using ScrubbyWeb.Models.Api;
using ScrubbyWeb.Services;

namespace ScrubbyWeb.Controllers
{
    [Route("api/da")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "API Key")]
    public class DataAggregationController : ControllerBase
    {
        private readonly IMongoCollection<ApiKeyModel> _apiKeys;
        private readonly IMongoCollection<LogMessage> _messages;
        private readonly IMongoCollection<Round> _rounds;

        public DataAggregationController(MongoAccess mongo)
        {
            _rounds = mongo.DB.GetCollection<Round>("rounds");
            _messages = mongo.DB.GetCollection<LogMessage>("logmessages");
            _apiKeys = mongo.DB.GetCollection<ApiKeyModel>("scrubby_api_keys");
        }

        private async Task<FailedRequest> ValidateAPICall(DataAggregationRequestModel model, ApiKeyModel apikey)
        {
            if (model.UpperRoundLimit < model.LowerRoundLimit)
                return new FailedRequest
                {
                    Message = "Invalid range, upper round lesser than lower round.",
                    Detail =
                        $"Requested rounds between {model.LowerRoundLimit} to {model.UpperRoundLimit}, this is an invalid range.",
                    Timestamp = DateTime.Now
                };

            var roundCount = (await GetFilesFromRounds(model)).Count();

            if (roundCount > apikey.MaxRoundRange)
                return new FailedRequest
                {
                    Message = "Round range exceeds API key limit",
                    Detail =
                        $"Your API key is limited to {apikey.MaxRoundRange} rounds in the selection range. You attempted to select {roundCount} rounds.",
                    Timestamp = DateTime.Now
                };

            if (apikey.RunningJobs >= apikey.MaxParallelCalls)
                return new FailedRequest
                {
                    Message = "Maximum parallel jobs reached",
                    Detail =
                        $"Your API key is limited to {apikey.MaxParallelCalls} parallel requests, you must wait for one of these to finish.",
                    Timestamp = DateTime.Now
                };

            return null;
        }

        private async Task<IEnumerable<BsonDocument>> GetFilesFromRounds(DataAggregationRequestModel model)
        {
            PipelineDefinition<Round, BsonDocument> roundsPipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("_id", new BsonDocument()
                        .Add("$gte", model.LowerRoundLimit)
                        .Add("$lte", model.UpperRoundLimit)
                    )),
                new BsonDocument("$project", new BsonDocument()
                    .Add("_id", 1.0)
                    .Add("Files", new BsonDocument()
                        .Add("$filter", new BsonDocument()
                            .Add("input", "$Files")
                            .Add("as", "file")
                            .Add("cond", new BsonDocument()
                                .Add("$in", new BsonArray()
                                    .Add("$$file.filename")
                                    .Add(new BsonArray()
                                        .AddRange(model.Files)
                                    )
                                )
                            )
                        )
                    ))
            };

            return await _rounds.Aggregate(roundsPipeline).ToListAsync();
        }

        private static async Task<List<BsonDocument>> DevelopLogMessageFilter(DataAggregationRequestModel model,
            ApiKeyModel apiKey, IEnumerable<ObjectId> files, int min)
        {
            var logmessagePipelineRaw = new List<BsonDocument>
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("ParentFile", new BsonDocument()
                        .Add("$in", new BsonArray()
                            .AddRange(files)
                        )
                    ))
            };

            if (model.TypeFilters != null && model.TypeFilters.Count > 0)
                logmessagePipelineRaw.First()["$match"].AsBsonDocument.Add("Type", new BsonDocument()
                    .Add("$in", new BsonArray()
                        .AddRange(model.TypeFilters)
                    ));

            if (model.ContentFilter != null)
                logmessagePipelineRaw.First()["$match"].AsBsonDocument.Add("Message", new BsonDocument()
                    .Add("$regex", model.ContentFilter));

            if (model.NoMetadata)
                logmessagePipelineRaw.Add(
                    new BsonDocument("$project", new BsonDocument()
                        .Add("Message", 1.0)
                        .Add("ParentFile", 1.0)
                        .Add("RelativeIndex", 1.0))
                );

            if (apiKey.MaxDocumentsReturned != -1)
                logmessagePipelineRaw.Add(
                    new BsonDocument("$limit", min)
                );

            return logmessagePipelineRaw;
        }

        private async Task ModifyAPIParallelCount(ApiKeyModel apiKey, bool increment)
        {
            UpdateDefinition<ApiKeyModel> update = null;
            var filter = Builders<ApiKeyModel>.Filter.Eq(x => x.Key, apiKey.Key);

            if (increment)
                update = Builders<ApiKeyModel>.Update.Inc(x => x.RunningJobs, 1).Inc(x => x.JobsRan, 1);
            else
                update = Builders<ApiKeyModel>.Update.Inc(x => x.RunningJobs, -1);

            await _apiKeys.FindOneAndUpdateAsync(filter, update);
        }

        private static async Task<JObject> WrapResponse(JArray data, int min, int responseSize, int rounds, int files)
        {
            dynamic innerMessage = new JObject();
            innerMessage.rounds = rounds;
            innerMessage.files = files;
            innerMessage.responseSize = responseSize;
            innerMessage.documentLimitEnforced = responseSize == min;

            dynamic toReturn = new JObject();
            toReturn.responseInfo = innerMessage;
            toReturn.data = data;

            return toReturn;
        }

        private static JObject GetRoundJObject(int roundid, IEnumerable<dynamic> messages)
        {
            var toReturn = new JObject
            {
                {"roundID", roundid},
                {"messages", JArray.FromObject(messages)}
            };

            return toReturn;
        }

        [HttpPost("aggregate")]
        public async Task<IActionResult> Aggregate([FromBody] DataAggregationRequestModel model)
        {
            var apiKey = new ObjectId(User.Claims.First(y => y.Type == "API Key").Value);
            var apiKeyRecord = await _apiKeys.Find(x => x.Key == apiKey).Limit(1).FirstOrDefaultAsync();
            var min = model.ResponseLimit != 0
                ? Math.Min(apiKeyRecord.MaxDocumentsReturned, model.ResponseLimit)
                : apiKeyRecord.MaxDocumentsReturned;

            var validation = await ValidateAPICall(model, apiKeyRecord);
            if (validation != null) return BadRequest(validation);

            await ModifyAPIParallelCount(apiKeyRecord, true);

            try
            {
                IActionResult toReturn;
                var roundsToAggregate = await GetFilesFromRounds(model);
                IEnumerable<ObjectId> files = new List<ObjectId>();
                files = roundsToAggregate.Aggregate(files,
                    (current, list) =>
                    {
                        return current.Concat(list["Files"].AsBsonArray.Select(x => x["_id"].AsObjectId));
                    });

                PipelineDefinition<LogMessage, LogMessage> logmessagePipeline =
                    (await DevelopLogMessageFilter(model, apiKeyRecord, files, min)).ToArray();

                var response = await _messages.Aggregate(logmessagePipeline).ToListAsync();

                if (model.GroupByRound)
                {
                    IEnumerable<JObject> altResponse = new List<JObject>();
                    var groupedData = response.GroupBy(x => x.ParentFile);

                    if (model.NoMetadata)
                    {
                        var test = groupedData.Select(x => (
                            roundid: roundsToAggregate.First(y =>
                                y["Files"].AsBsonArray.Any(z => z["_id"].AsObjectId == x.Key))["_id"].AsInt32,
                            messages: x.Select(a => a.Message)));
                        altResponse = altResponse.Concat(test.OrderBy(x => x.roundid)
                            .Select(x => GetRoundJObject(x.roundid, x.messages)));
                        toReturn = Ok(await WrapResponse(JArray.FromObject(altResponse), min, response.Count,
                            roundsToAggregate.Count(), files.Count()));
                    }
                    else
                    {
                        var test = groupedData.Select(x => (
                            roundid: roundsToAggregate.First(y =>
                                y["Files"].AsBsonArray.Any(z => z["_id"].AsObjectId == x.Key))["_id"].AsInt32,
                            messages: x.ToList()));
                        altResponse = altResponse.Concat(test.OrderBy(x => x.roundid)
                            .Select(x => GetRoundJObject(x.roundid, x.messages)));
                        toReturn = Ok(await WrapResponse(JArray.FromObject(altResponse), min, response.Count,
                            roundsToAggregate.Count(), files.Count()));
                    }
                }
                else if (model.NoMetadata)
                {
                    toReturn = Ok(await WrapResponse(JArray.FromObject(response.Select(x => x.Message)), min,
                        response.Count, roundsToAggregate.Count(), files.Count()));
                }
                else
                {
                    toReturn = Ok(await WrapResponse(JArray.FromObject(response), min, response.Count,
                        roundsToAggregate.Count(), files.Count()));
                }

                await ModifyAPIParallelCount(apiKeyRecord, false);
                return toReturn;
            }
            catch (Exception)
            {
                // Ensure we decrement the running jobs
                await ModifyAPIParallelCount(apiKeyRecord, false);
                throw;
            }
        }
    }
}