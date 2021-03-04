using System;
using System.Collections.Generic;
using System.Linq;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models.Data;

namespace ScrubbyWeb.Models
{
    public class CKeyModel
    {
        public List<(string, int)> Names { get; set; }
        public List<ServerStatistic> Playtime { get; set; }
        public List<int> Rounds { get; set; }
        public CKey Key { get; set; }
        public TimeSpan TotalTimeConnected => TimeSpan.FromTicks(Playtime.Sum(x => x.TotalTimeConnected.Ticks));
        public TimeSpan TotalTimePlayed => TimeSpan.FromTicks(Playtime.Sum(x => x.PlayedTime.Ticks));
    }
}