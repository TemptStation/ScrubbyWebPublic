using ScrubbyCommon.Data;

namespace ScrubbyWeb.Models
{
    public class RoundRuntimeModel
    {
        public int RoundID { get; set; }
        public RoundBuildInfo Version { get; set; }
    }
}