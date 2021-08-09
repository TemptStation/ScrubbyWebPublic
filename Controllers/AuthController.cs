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

namespace ScrubbyWeb.Controllers
{
    public class AuthController : Controller
    {
        private readonly string _httpSafeSecretToken;
        private readonly IUserService _users;

        public AuthController(IConfiguration config, IUserService users)
        {
            var secretToken = config.GetSection("SecretTokens").GetSection("tgauthapi").Value;
            _httpSafeSecretToken = HttpUtility.UrlEncode(secretToken);
            _users = users;
        }

        private async Task<TgSessionResponse> InitializeAuthHandshake()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var returnAddress = HttpUtility.UrlEncode(Url.Action("FinishLogin", "Auth", null, Request.Scheme));
                    var response = await client.GetAsync(
                        $"https://tgstation13.org/phpBB/oauth_create_session.php?site_private_token={_httpSafeSecretToken}&return_uri={returnAddress}");
                    response.EnsureSuccessStatusCode();

                    var stringResult = await response.Content.ReadAsStringAsync();
                    dynamic rawData = JObject.Parse(stringResult);
                    return new TgSessionResponse
                    {
                        Status = rawData.status, SessionPrivateToken = rawData.session_private_token,
                        SessionPublicToken = rawData.session_public_token
                    };
                }
                catch (HttpRequestException e)
                {
                    throw new Exception("Failed to initialize handshake with TGstation forum!", e);
                }
            }
        }

        [HttpGet("/login")]
        public async Task<IActionResult> Login()
        {
            var initialData = await InitializeAuthHandshake();
            HttpContext.Session.SetString("AuthSessionPublicToken", initialData.SessionPublicToken);
            HttpContext.Session.SetString("AuthSessionPrivateToken", initialData.SessionPrivateToken);
            await HttpContext.Session.CommitAsync();

            var httpSafePublicToken = HttpUtility.UrlEncode(initialData.SessionPublicToken);
            return Redirect($"https://tgstation13.org/phpBB/oauth.php?session_public_token={httpSafePublicToken}");
        }

        private async Task<TgUserDataResponse> FinalizeAuthentication()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    await HttpContext.Session.LoadAsync();
                    var sessionPrivateToken = HttpContext.Session.GetString("AuthSessionPrivateToken");
                    var httpSafePrivateToken = HttpUtility.UrlEncode(sessionPrivateToken);
                    var response = await client.GetAsync(
                        $"https://tgstation13.org/phpBB/oauth_get_session_info.php?site_private_token={_httpSafeSecretToken}&session_private_token={httpSafePrivateToken}");
                    response.EnsureSuccessStatusCode();

                    var stringResult = await response.Content.ReadAsStringAsync();
                    dynamic rawData = JObject.Parse(stringResult);
                    return new TgUserDataResponse
                    {
                        Status = rawData.status,
                        PhpBBUsername = rawData.phpbb_username,
                        ByondKey = rawData.byond_key,
                        ByondCKey = rawData.byond_ckey
                    };
                }
                catch (HttpRequestException e)
                {
                    throw new Exception("Failed to finalize handshake with TGstation forum!", e);
                }
            }
        }

        [HttpGet("/auth/finalize")]
        public async Task<IActionResult> FinishLogin(Uri redirectUrl = null)
        {
            var user = await FinalizeAuthentication();

            if (user == null || user.Status == "error" || user.PhpBBUsername == "null")
                return BadRequest();
            
            var foundUser = await _users.GetUser(user.PhpBBUsername) ?? await _users.CreateUser(new ScrubbyUser()
            {
                PhpBBUsername = user.PhpBBUsername,
                ByondKey = user.ByondKey,
                ByondCKey = user.ByondCKey,
                Roles = new List<string>
                {
                    "User"
                }
            });

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.PhpBBUsername),
                new Claim("CKey", user.ByondCKey)
            };
            claims.AddRange(foundUser.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(5)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return Redirect(redirectUrl?.ToString() ?? Url.Action("Me", "User"));
        }

        [HttpGet("/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect(Url.Action("Index", "Home"));
        }

        private class TgSessionResponse
        {
            public string Status { get; set; }
            public string SessionPrivateToken { get; set; }
            public string SessionPublicToken { get; set; }
        }

        public class TgUserDataResponse
        {
            public string Status { get; set; }
            public string PhpBBUsername { get; set; }
            public string ByondKey { get; set; }
            public string ByondCKey { get; set; }
        }
    }
}