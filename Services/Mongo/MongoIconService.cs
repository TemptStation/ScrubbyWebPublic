using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using ScrubbyWeb.Models;
using ScrubbyWeb.Services.Interfaces;
using File = ScrubbyCommon.Data.File;

namespace ScrubbyWeb.Services.Mongo
{
    public class MongoIconService : IIconService
    {
        private readonly GridFSBucket _DMIFiles;
        private readonly GridFSBucket _DMIGifs;
        private readonly IMongoCollection<MongoDMIFile> _files;
        private readonly IMongoCollection<MongoDMIState> _states;

        public MongoIconService(MongoAccess mongo)
        {
            _states = mongo.DB.GetCollection<MongoDMIState>("scrubby_dmi_state");
            _files = mongo.DB.GetCollection<MongoDMIFile>("scrubby_dmi_file_metadata");
            _DMIFiles = new GridFSBucket(mongo.DB, new GridFSBucketOptions {BucketName = "scrubby_dmi_files"});
            _DMIGifs = new GridFSBucket(mongo.DB, new GridFSBucketOptions {BucketName = "scrubby_dmi_gifs"});
        }

        public async Task<File> GetIcon(dynamic id)
        {
            var fid = new ObjectId(id);
            var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", fid);
            using var cursor = await _DMIFiles.FindAsync(filter);
            var metadata = await cursor.FirstOrDefaultAsync();
            return metadata != null
                ? new File()
                {
                    FileName = metadata.Filename,
                    UploadDate = metadata.UploadDateTime,
                    Length = metadata.Length,
                    _id = metadata.Id
                }
                : null;
        }

        public async Task<File> GetGif(dynamic id)
        {
            var fid = new ObjectId(id);
            var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", fid);
            using var cursor = await _DMIGifs.FindAsync(filter);
            var metadata = await cursor.FirstOrDefaultAsync();
            return metadata != null
                ? new File()
                {
                    FileName = metadata.Filename,
                    UploadDate = metadata.UploadDateTime,
                    Length = metadata.Length,
                    _id = metadata.Id
                }
                : null;
        }

        public async Task<Stream> GetIconContent(dynamic id)
        {
            var fid = new ObjectId(id);
            var stream = new MemoryStream();
            await _DMIFiles.DownloadToStreamAsync(fid, stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public async Task<Stream> GetGifContent(dynamic id)
        {
            var fid = new ObjectId(id);
            var stream = new MemoryStream();
            await _DMIGifs.DownloadToStreamAsync(fid, stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public async Task<MongoDMIFile> SearchForPath(string path)
        {
            var filter = Builders<MongoDMIFile>.Filter.Or(new List<FilterDefinition<MongoDMIFile>>
            {
                Builders<MongoDMIFile>.Filter.Eq(x => x.Name, path),
                Builders<MongoDMIFile>.Filter.Eq(x => x.Name, "/" + path)
            });
            using var cursor = await _files.FindAsync(filter);
            return await cursor.FirstOrDefaultAsync();
        }

        public async Task<List<MongoDMIState>> GetStates(dynamic fileId)
        {
            var fid = (ObjectId) fileId;
            return await _states.Find(x => x.PFileID == fid).ToListAsync();
        }

        public async Task<List<MongoDMIState>> SearchStates(Regex pattern)
        {
            return await _states.Find(x => pattern.IsMatch(x.Name)).Limit(100).ToListAsync();
        }
    }
}