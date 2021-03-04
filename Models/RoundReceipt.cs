using System;
using ScrubbyCommon.Data;
using ScrubbyCommon.Data.Events;

namespace ScrubbyWeb.Models
{
    public class RoundReceipt
    {
        public int RoundID { get; set; }
        public string Job { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan ConnectedTime { get; set; }
        public bool RoundStartPlayer { get; set; }
        public bool PlayedInRound { get; set; }
        public bool Antagonist { get; set; }
        public bool RoundStartSuicide { get; set; }
        public bool IsSecurity => Job == "security officer" || Job == "head of security" || Job == "warden";
        public Suicide FirstSuicide { get; set; }
        public LogMessage FirstSuicideEvidence { get; set; }
        public string Name { get; set; }
        public string Server { get; set; }
    }
}