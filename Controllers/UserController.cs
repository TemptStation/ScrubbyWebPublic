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
            if (await _users.GetUser(User.Identity?.Name) == null)
                return Unauthorized("You are not logged in");
            return View("User");
        }
    }
}