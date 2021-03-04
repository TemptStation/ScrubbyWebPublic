using System.Collections.Generic;
using ScrubbyCommon.Data;

namespace ScrubbyWeb.Models
{
    public class RoundRuntimeModel
    {
        public int RoundID { get; set; }
        public RoundBuildInfo Version { get; set; }
        public List<Runtime> Runtimes { get; set; }
    }
}