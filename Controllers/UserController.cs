using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ScrubbyWeb.Models;
using ScrubbyWeb.Services;
using ScrubbyWeb.Services.Mongo;

namespace ScrubbyWeb.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly IMongoCollection<ApiKeyModel> _apiKeys;
        private readonly IMongoCollection<AuthenticationRecordModel> _users;

        public UserController(MongoAccess mongo)
        {
            _users = mongo.DB.GetCollection<AuthenticationRecordModel>("scrubby_users");
            _apiKeys = mongo.DB.GetCollection<ApiKeyModel>("scrubby_api_keys");
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var uname = User.Identity.Name;
            var user = await _users.Find(x => x.PhpBBUsername == uname).FirstAsync();
            var keys = await _apiKeys.Find(x => user.APIKeys.Contains(x.Key)).ToListAsync();

            return View("User", new ScrubbyUserModel {User = user, APIKeys = keys});
        }
    }
}