using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MimeMapping;
using ScrubbyWeb.Services.Interfaces;
using SCD = ScrubbyCommon.Data;

namespace ScrubbyWeb.Controllers
{
    public class ImageController : Controller
    {
        private readonly IFileService _files;

        private readonly List<string> _validPictures = new List<string>
        {
            "png",
            "jpeg",
            "jpg"
        };

        public ImageController(IFileService files)
        {
            _files = files;
        }

        [HttpGet("image/{id:int}")]
        public async Task<FileStreamResult> FetchImage(int id)
        {
            var f = await _files.GetFile(id);
            if (!_validPictures.Contains(f.Name.Split(".").Last()) || f.Size == 0) return null;
            var mime = MimeUtility.GetMimeMapping(f.Name);
            var stream = await _files.GetFileContent(id);
            return new FileStreamResult(stream, mime);
        }
    }
}