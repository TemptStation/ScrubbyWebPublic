using System.Text.RegularExpressions;

namespace ScrubbyWeb.Models.PostRequests
{
    public enum PlayerSearchType
    {
        Unknown,
        CKey,
        ICName
    }

    public class PlayerSearchPostModel
    {
        public Regex Regex { get; set; }
        public PlayerSearchType SearchType { get; set; }
    }
}