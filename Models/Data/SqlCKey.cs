using System;
using System.Text.RegularExpressions;

namespace ScrubbyWeb.Models.Data
{
    public class SqlCKey
    {
        public string CKey { get; set; }
        public string ByondKey { get; set; }
        public DateTime? JoinedBYOND { get; set; }
        public bool UserNotFound { get; set; }
        public bool UserInactive { get; set; }
        private static readonly Regex CKeyInvalidCharacters = new Regex(@"[^a-z0-9]", RegexOptions.Compiled);
        public static string SanitizeKey(string raw)
        {
            return CKeyInvalidCharacters.Replace(raw.ToLower(), "");
        }
    }
}