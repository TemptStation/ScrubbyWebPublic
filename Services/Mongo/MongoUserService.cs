using System.Threading.Tasks;
using MongoDB.Driver;
using ScrubbyWeb.Models;
using ScrubbyWeb.Models.Data;
using ScrubbyWeb.Services.Interfaces;

namespace ScrubbyWeb.Services.Mongo
{
    public class MongoUserService : IUserService
    {
        private readonly IMongoCollection<AuthenticationRecordModel> _users;

        public MongoUserService(MongoAccess mongo)
        {
            _users = mongo.DB.GetCollection<AuthenticationRecordModel>("scrubby_users");
        }

        public async Task<ScrubbyUser> GetUser(string phpbbUsername)
        {
            var mongoResult = (await _users.FindAsync(x => x.PhpBBUsername == phpbbUsername)).FirstOrDefault();
            if (mongoResult == null)
                return null;
            return new ScrubbyUser()
            {
                ByondCKey = mongoResult.ByondCKey,
                ByondKey = mongoResult.ByondKey,
                Roles = mongoResult.Roles,
                PhpBBUsername = mongoResult.PhpBBUsername
            };
        }

        public async Task<ScrubbyUser> CreateUser(ScrubbyUser user)
        {
            await _users.InsertOneAsync(new AuthenticationRecordModel()
            {
                ByondCKey = user.ByondCKey,
                ByondKey = user.ByondKey,
                PhpBBUsername = user.PhpBBUsername,
                Roles = user.Roles
            });

            return await GetUser(user.PhpBBUsername);
        }

        public Task<ScrubbyUser> UpdateUser(ScrubbyUser user)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> DeleteUser(ScrubbyUser user)
        {
            throw new System.NotImplementedException();
        }
    }
}