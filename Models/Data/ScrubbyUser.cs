using System.Collections.Generic;

namespace ScrubbyWeb.Models.Data
{
    public class ScrubbyUser
    {
        public string PhpBBUsername { get; set; }
        public string ByondKey { get; set; }
        public string ByondCKey { get; set; }
        public List<string> Roles { get; set; }
    }
}