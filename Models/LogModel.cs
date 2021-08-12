using ScrubbyCommon.Data;
using ScrubbyWeb.Models.Data;
using ScrubbyWeb.Models.PostRequests;

namespace ScrubbyWeb.Models
{
    public class LogModel
    {
        public ScrubbyRound Parent { get; set; }
        public FileMessagePostModel Data { get; set; }
    }
}