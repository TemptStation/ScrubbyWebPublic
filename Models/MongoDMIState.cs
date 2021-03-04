using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScrubbyWeb.Models
{
    public class MongoDMIState
    {
        [BsonId] public ObjectId StateID { get; set; }

        public List<MongoDMIFrame> Frames { get; set; }
        public int FrameCount { get; set; }
        public int DirCount { get; set; }
        public string ParentFile { get; set; }
        public ObjectId PFileID { get; set; }
        public List<ObjectId> RenderedAnimations { get; set; }
        public string Name { get; set; }
        public bool Animated { get; set; }
        public double[] Delay { get; set; }
        public bool Rewind { get; set; }
        public bool Movement { get; set; }
        public int Loop { get; set; }
        public List<double[]> Hotspots { get; set; }
    }
}