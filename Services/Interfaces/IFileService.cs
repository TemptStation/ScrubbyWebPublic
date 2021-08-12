using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ScrubbyCommon.Data;
using ScrubbyWeb.Models.Data;
using ScrubbyWeb.Models.PostRequests;

namespace ScrubbyWeb.Services.Interfaces
{
    public interface IFileService
    {
        public Task<IEnumerable<LogMessage>> GetMessages(FileMessagePostModel model);
        public Task<ScrubbyFile> GetFile(int id);
        public Task<Stream> GetFileContent(int id);
    }
}