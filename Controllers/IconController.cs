using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using ScrubbyWeb.Models;
using ScrubbyWeb.Services.Mongo;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace ScrubbyWeb.Controllers
{
    public class IconController : Controller
    {
        private readonly GridFSBucket _DMIFiles;
        private readonly GridFSBucket _DMIGifs;
        private readonly IMongoCollection<MongoDMIFile> _files;
        private readonly IMongoCollection<MongoDMIState> _states;

        public IconController(MongoAccess mongo)
        {
            _states = mongo.DB.GetCollection<MongoDMIState>("scrubby_dmi_state");
            _files = mongo.DB.GetCollection<MongoDMIFile>("scrubby_dmi_file_metadata");
            _DMIFiles = new GridFSBucket(mongo.DB, new GridFSBucketOptions {BucketName = "scrubby_dmi_files"});
            _DMIGifs = new GridFSBucket(mongo.DB, new GridFSBucketOptions {BucketName = "scrubby_dmi_gifs"});
        }

        [HttpGet("icon/{id}")]
        public async Task<IActionResult> GetIcon(string id, [FromQuery(Name = "x")] int x = -1,
            [FromQuery(Name = "y")] int y = -1)
        {
            var fid = new ObjectId(id);
            var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", fid);

            using (var cursor = await _DMIFiles.FindAsync(filter))
            {
                var metadata = await cursor.FirstOrDefaultAsync();
                if (metadata != null)
                {
                    var stream = new MemoryStream();
                    await _DMIFiles.DownloadToStreamAsync(fid, stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    if (x < -1) x = -1;
                    if (y < -1) y = -1;

                    if (x != -1 || y != -1)
                        using (var image = Image.Load<Rgba32>(stream, out var format))
                        {
                            x = Math.Min(x, 512);
                            y = Math.Min(y, 512);

                            if (x == -1 || y == -1)
                            {
                                if (x == -1)
                                    image.Mutate(ctx => ctx.Resize((int) (y * 1.0 / image.Height * image.Width), y,
                                        new NearestNeighborResampler()));
                                else
                                    image.Mutate(ctx => ctx.Resize(x, (int) (x * 1.0 / image.Width * image.Height),
                                        new NearestNeighborResampler()));
                            }
                            else
                            {
                                image.Mutate(ctx => ctx.Resize(x, y, new NearestNeighborResampler()));
                            }

                            stream.Seek(0, SeekOrigin.Begin);
                            image.Save(stream, format);
                            stream.Seek(0, SeekOrigin.Begin);
                        }

                    var toRespond = new FileStreamResult(stream, "image/png");
                    var cd = new ContentDisposition
                    {
                        Inline = true,
                        FileName = metadata.Filename
                    };
                    Response.Headers["Content-Disposition"] = cd.ToString();
                    Response.Headers["X-Content-Type-Options"] = "nosniff";
                    return toRespond;
                }

                return NotFound("Invalid file ID given.");
            }
        }

        [HttpGet("icon/animated/{id}")]
        public async Task<IActionResult> GetGif(string id)
        {
            var fid = new ObjectId(id);
            var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", fid);

            using (var cursor = await _DMIGifs.FindAsync(filter))
            {
                var metadata = await cursor.FirstOrDefaultAsync();
                if (metadata != null)
                {
                    var stream = new MemoryStream();
                    await _DMIGifs.DownloadToStreamAsync(fid, stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    var toRespond = new FileStreamResult(stream, "image/gif");
                    var cd = new ContentDisposition
                    {
                        Inline = true,
                        FileName = metadata.Filename
                    };
                    Response.Headers["Content-Disposition"] = cd.ToString();
                    Response.Headers["X-Content-Type-Options"] = "nosniff";
                    return toRespond;
                }

                return NotFound("Invalid file ID given.");
            }
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

            var searchResult = await _states.Find(x => regex.IsMatch(x.Name)).Limit(100).ToListAsync();

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

            var filter = Builders<MongoDMIFile>.Filter.Or(new List<FilterDefinition<MongoDMIFile>>
            {
                Builders<MongoDMIFile>.Filter.Eq(x => x.Name, path),
                Builders<MongoDMIFile>.Filter.Eq(x => x.Name, "/" + path)
            });
            using (var cursor = await _files.FindAsync(filter))
            {
                var file = await cursor.FirstOrDefaultAsync();

                if (file != null)
                {
                    // dmi file exists
                    var toReturn = new DMIViewModel
                    {
                        File = file,
                        States = await _states.Find(x => x.PFileID == file.FileID).ToListAsync()
                    };

                    return View("SpriteSheet", toReturn);
                }

                return NotFound();
            }
        }
    }
}