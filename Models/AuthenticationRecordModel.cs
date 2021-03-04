using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScrubbyWeb.Models
{
    public class AuthenticationRecordModel
    {
        [BsonId] public string PhpBBUsername { get; set; }

        public string ByondKey { get; set; }
        public string ByondCKey { get; set; }
        public List<string> Roles { get; set; }
        public List<ObjectId> APIKeys { get; set; }
    }
}