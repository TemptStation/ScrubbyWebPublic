using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MimeMapping;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using ScrubbyWeb.Services.Mongo;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SCD = ScrubbyCommon.Data;

namespace ScrubbyWeb.Controllers
{
    public class ImageController : Controller
    {
        private readonly IMongoCollection<SCD.File> _filedata;
        private readonly GridFSBucket _files;

        private readonly List<string> validPictures = new List<string>
        {
            "png",
            "jpeg",
            "jpg"
        };

        public ImageController(MongoAccess mongo)
        {
            _filedata = mongo.DB.GetCollection<SCD.File>("rawlogs.files");
            _files = new GridFSBucket(mongo.DB, new GridFSBucketOptions
            {
                BucketName = "rawlogs"
            });
        }

        [HttpGet("image/{id}")]
        public FileStreamResult FetchImage(string id, [FromQuery(Name = "x")] int x = -1,
            [FromQuery(Name = "y")] int y = -1)
        {
            var fid = new ObjectId(id);
            var f = _filedata.Find(z => z._id == fid).Limit(1).FirstOrDefault();

            if (!validPictures.Contains(f.FileName.Split(".").Last()) || f.Length == 0) return null;

            var mime = MimeUtility.GetMimeMapping(f.FileName);
            var stream = new MemoryStream();
            _files.DownloadToStream(fid, stream);
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
                            image.Mutate(ctx => ctx.Resize((int) (y * 1.0 / image.Height * image.Width), y));
                        else
                            image.Mutate(ctx => ctx.Resize(x, (int) (x * 1.0 / image.Width * image.Height)));
                    }
                    else
                    {
                        image.Mutate(ctx => ctx.Resize(x, y));
                    }

                    stream.Seek(0, SeekOrigin.Begin);
                    image.Save(stream, format);
                    stream.Seek(0, SeekOrigin.Begin);
                }

            return new FileStreamResult(stream, mime);
        }

        [HttpGet("image/random")]
        public FileStreamResult RandomImage([FromQuery(Name = "x")] int x = -1, [FromQuery(Name = "y")] int y = -1)
        {
            PipelineDefinition<SCD.File, SCD.File> pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument()
                    .Add("filename", new BsonDocument()
                        .Add("$regex", "^photos")
                    )
                    .Add("length", new BsonDocument()
                        .Add("$gt", 0)
                    )),
                new BsonDocument("$sample", new BsonDocument()
                    .Add("size", 1.0))
            };

            var selected = _filedata.Aggregate(pipeline).ToList().FirstOrDefault();

            if (selected == null || selected.Length == 0) return null;
            return FetchImage(selected._id.ToString(), x, y);
        }
    }
}