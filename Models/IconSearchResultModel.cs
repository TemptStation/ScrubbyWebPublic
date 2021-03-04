using System.Collections.Generic;

namespace ScrubbyWeb.Models
{
    public class IconSearchResultModel
    {
        public List<MongoDMIState> States { get; set; }
        public string SearchQuery { get; set; }
    }
}