using ScrubbyCommon.Data;
using ScrubbyWeb.Models.PostRequests;

namespace ScrubbyWeb.Models
{
    public class LogModel
    {
        public Round Parent { get; set; }
        public FileMessagePostModel Data { get; set; }
    }
}