using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScrubbyWeb.Services.Interfaces;

namespace ScrubbyWeb.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _users;

        public UserController(IUserService users)
        {
            _users = users;
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var uname = User.Identity.Name;
            var foundUser = await _users.GetUser(uname);
            if (foundUser == null)
                return Unauthorized("You are not logged in");
            return View("User", foundUser);
        }
    }
}