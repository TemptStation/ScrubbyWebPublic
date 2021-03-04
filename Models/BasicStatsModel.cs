using System;
using System.Collections.Generic;

namespace ScrubbyWeb.Models
{
    public class BasicStatsModel
    {
        public BasicStatsModel(int fr, DateTime frt, List<(string, DateTime, int)> lr, int rc, int fc)
        {
            FirstRound = fr;
            FirstRoundTime = frt;
            LatestRounds = lr;
            RoundCount = rc;
            FileCount = fc;
        }

        public int FirstRound { get; set; }
        public DateTime FirstRoundTime { get; set; }
        public List<(string, DateTime, int)> LatestRounds { get; set; }
        public int RoundCount { get; set; }
        public int FileCount { get; set; }
    }
}