using System.Collections.Generic;
using ScrubbyCommon.Data;

namespace ScrubbyWeb.Models
{
    public class NewsCasterModel
    {
        public Round Round { get; set; }
        public List<NewsCasterChannel> Channels { get; set; }
        public List<NewsCasterWanted> Wanted { get; set; }
    }
}