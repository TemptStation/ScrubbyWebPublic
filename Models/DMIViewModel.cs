using System.Collections.Generic;

namespace ScrubbyWeb.Models
{
    public class DMIViewModel
    {
        public MongoDMIFile File { get; set; }
        public List<MongoDMIState> States { get; set; }
    }
}