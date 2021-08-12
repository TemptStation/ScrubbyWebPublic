using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ScrubbyWeb.Models;
using ScrubbyWeb.Services.Interfaces;

namespace ScrubbyWeb.Controllers
{
    public class IconController : Controller
    {
        private readonly IIconService _icons;

        public IconController(IIconService icons)
        {
            _icons = icons;
        }

        [HttpGet("icon/{id}")]
        public async Task<IActionResult> GetIcon(string id)
        {
            var file = await _icons.GetIcon(id);
            if (file == null)
                return NotFound("Invalid file ID given.");
            var stream = await _icons.GetIconContent(id);
            var toRespond = new FileStreamResult(stream, "image/png");
            var cd = new ContentDisposition
            {
                Inline = true,
                FileName = file.FileName
            };
            Response.Headers["Content-Disposition"] = cd.ToString();
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return toRespond;
        }

        [HttpGet("icon/animated/{id}")]
        public async Task<IActionResult> GetGif(string id)
        {
            var file = await _icons.GetGif(id);
            if (file == null)
                return NotFound("Invalid file ID given.");
            var stream = await _icons.GetGifContent(id);
            var toRespond = new FileStreamResult(stream, "image/gif");
            var cd = new ContentDisposition
            {
                Inline = true,
                FileName = file.FileName
            };
            Response.Headers["Content-Disposition"] = cd.ToString();
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return toRespond;
        }

        [HttpGet("icon/search/{searchTerm?}")]
        public async Task<IActionResult> SearchIcon(string searchTerm)
        {
            if (searchTerm == null) return View("SearchResult", new IconSearchResultModel());

            Regex regex = null;
            try
            {
                regex = new Regex(searchTerm, RegexOptions.IgnoreCase);
            }
            catch (Exception)
            {
                return View("SearchResult",
                    new IconSearchResultModel {States = new List<MongoDMIState>(), SearchQuery = searchTerm});
            }

            var searchResult = await _icons.SearchStates(regex);

            return View("SearchResult", new IconSearchResultModel {States = searchResult, SearchQuery = searchTerm});
        }

        [HttpPost("iconsearch")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SearchIconPost([FromForm] string query)
        {
            return RedirectToAction("SearchIcon", new {searchTerm = query});
        }

        [HttpGet("/dmi/{*path}")]
        public async Task<IActionResult> GetSpriteSheet(string path)
        {
            if (path == null) return NotFound();
            var file = await _icons.SearchForPath(path);
            if (file != null)
            {
                // dmi file exists
                var toReturn = new DMIViewModel
                {
                    File = file,
                    States = await _icons.GetStates(file.FileID)
                };

                return View("SpriteSheet", toReturn);
            }

            return NotFound();
        }
    }
}