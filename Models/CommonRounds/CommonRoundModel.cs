using System;

namespace ScrubbyWeb.Models.CommonRounds
{
    public class CommonRoundModel
    {
        public int Round { get; set; }
        public DateTime? Started { get; set; }
        public DateTime? Ended { get; set; }
    }
}