using System.Collections.Generic;

namespace ScrubbyWeb.Models
{
    public class ScrubbyUserModel
    {
        public IEnumerable<ApiKeyModel> APIKeys { get; set; }
        public AuthenticationRecordModel User { get; set; }
    }
}