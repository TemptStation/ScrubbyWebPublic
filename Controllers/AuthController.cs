using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using ScrubbyWeb.Models.Data;
using ScrubbyWeb.Services.Interfaces;
using Tgstation.Auth;

namespace ScrubbyWeb.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserService _users;

        public AuthController(IUserService users)
        {
            _users = users;
        }


        [HttpGet("/login")]
        public async Task<IActionResult> Login()
        {
            if (User.Identity is not { IsAuthenticated: true })
                await HttpContext.ChallengeAsync();
            return RedirectToAction("Me", "User");
        }

        [HttpGet("/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}