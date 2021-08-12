using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ScrubbyWeb.Models;
using File = ScrubbyCommon.Data.File;

namespace ScrubbyWeb.Services.Interfaces
{
    public interface IIconService
    {
        public Task<File> GetIcon(dynamic id);
        public Task<File> GetGif(dynamic id);
        public Task<Stream> GetIconContent(dynamic id);
        public Task<Stream> GetGifContent(dynamic id);
        public Task<MongoDMIFile> SearchForPath(string path);
        public Task<List<MongoDMIState>> GetStates(dynamic fileId);
        public Task<List<MongoDMIState>> SearchStates(Regex pattern);
    }
}