using System.Collections.Generic;

namespace ScrubbyWeb.Models.Api
{
    public class RoundsForCKeyAggregationModel
    {
        public List<string> CKeys { get; set; }
        public int Limit { get; set; } = -1;
        public bool GTERound { get; set; } = true;
        public int StartingRound { get; set; } = -1;
    }
}