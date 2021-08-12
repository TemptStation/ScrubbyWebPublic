using System;
using System.Collections.Generic;

namespace ScrubbyWeb.Models
{
    public class BasicStatsModel
    {
        public List<LatestRound> LatestRounds { get; set; }
        public int RoundCount { get; set; }
        public int FileCount { get; set; }
    }

    public class LatestRound
    {
        public string Server { get; set; }
        public DateTime Started { get; set; }
        public int Id { get; set; }
    }
}