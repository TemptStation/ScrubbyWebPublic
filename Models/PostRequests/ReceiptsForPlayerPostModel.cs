using System;

namespace ScrubbyWeb.Models.PostRequests
{
    public class ReceiptsForPlayerPostModel
    {
        public string CKey { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}