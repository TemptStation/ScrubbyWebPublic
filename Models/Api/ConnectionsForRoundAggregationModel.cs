using System.Collections.Generic;

namespace ScrubbyWeb.Models.Api
{
    public class ConnectionsForRoundAggregationModel
    {
        public int Round { get; set; }
        public List<string> CKeyFilter { get; set; }
    }
}