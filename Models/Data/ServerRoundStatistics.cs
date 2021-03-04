using System;

namespace ScrubbyWeb.Models.Data
{
    public struct ServerRoundStatistic
    {
        public string Server { get; set; }
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}