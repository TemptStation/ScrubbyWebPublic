using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ScrubbyWeb.Models.Data
{
    [BsonIgnoreExtraElements]
    public class ScrubbyAnnouncement
    {
        public DateTime Active { get; set; }
        public DateTime Expires { get; set; }
        public string Message { get; set; }
    }
}