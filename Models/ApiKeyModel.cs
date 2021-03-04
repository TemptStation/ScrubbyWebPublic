using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScrubbyWeb.Models
{
    public class ApiKeyModel
    {
        [BsonId] public ObjectId Key { get; set; }

        public string Name { get; set; }
        public int MaxParallelCalls { get; set; }
        public int MaxRoundRange { get; set; }
        public int MaxDocumentsReturned { get; set; }
        public int RunningJobs { get; set; }
        public int JobsRan { get; set; }
    }
}