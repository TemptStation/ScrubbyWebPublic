using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScrubbyWeb.Models
{
    public class MongoDMIFile
    {
        [BsonId] public ObjectId FileID { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public double Version { get; set; }
        public string Name { get; set; }
    }
}