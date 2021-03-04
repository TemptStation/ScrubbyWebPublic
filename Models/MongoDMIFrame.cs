using MongoDB.Bson;

namespace ScrubbyWeb.Models
{
    public class MongoDMIFrame
    {
        public ObjectId File { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int FrameIndex { get; set; }
        public int DirIndex { get; set; }
    }
}