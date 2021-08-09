using System.Collections.Generic;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Models
{
    public class CKeyModel
    {
        public List<NameCountRecord> Names { get; set; }
        public List<ServerStatistic> Playtime { get; set; }
        public CKey Key { get; set; }
        public string ByondKey { get; set; }
    }
}