using System.Collections.Generic;

namespace ScrubbyWeb.Models
{
    public class NameSearchResultModel
    {
        public List<PlayerNameStatistic> Statistics { get; set; }
        public string SearchTerm { get; set; }
    }
}