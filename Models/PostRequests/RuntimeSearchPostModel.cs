using System;

namespace ScrubbyWeb.Models.PostRequests
{
    public class RuntimeSearchPostModel<T>
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public T Data { get; set; }
    }
}