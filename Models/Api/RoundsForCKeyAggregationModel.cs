﻿using System.Collections.Generic;

namespace ScrubbyWeb.Models.Api
{
    public class RoundsForCKeyAggregationModel
    {
        public List<string> CKeys { get; set; }
        public int? Limit { get; set; }
        public bool GTERound { get; set; }
        public int? StartingRound { get; set; }
    }
}