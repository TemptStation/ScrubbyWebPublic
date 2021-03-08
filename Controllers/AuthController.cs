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
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using ScrubbyWeb.Models;
using ScrubbyWeb.Services;

namespace ScrubbyWeb.Controllers
{
    public class AuthController : Controller
    {
        private readonly IMongoCollection<ApiKeyModel> _apiKeys;

        private readonly string _httpSafeSecretToken;
        private readonly IMongoCollection<AuthenticationRecordModel> _users;

        public AuthController(IConfiguration config, MongoAccess mongo)
        {
            var secretToken = config.GetSection("SecretTokens").GetSection("tgauthapi").Value;
            _httpSafeSecretToken = HttpUtility.UrlEncode(secretToken);
            _users = mongo.DB.GetCollection<AuthenticationRecordModel>("scrubby_users");
            _apiKeys = mongo.DB.GetCollection<ApiKeyModel>("scrubby_api_keys");
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
                        Status = rawData.status,
                        SessionPrivateToken = rawData.session_private_token,
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
                return View("LoginFailed");

            var usersearch = (await _users.FindAsync(x => x.PhpBBUsername == user.PhpBBUsername)).FirstOrDefault();

            if (usersearch != null)
            {
                var filter =
                    Builders<AuthenticationRecordModel>.Filter.Eq(x => x.PhpBBUsername, usersearch.PhpBBUsername);
                UpdateDefinition<AuthenticationRecordModel> update = null;

                if (usersearch.APIKeys == null || usersearch.APIKeys.Count < 0)
                {
                    // Needs more API key!
                    var toInsert = new ApiKeyModel
                    {
                        Name = usersearch.PhpBBUsername,
                        MaxDocumentsReturned = 100000,
                        MaxParallelCalls = 2,
                        MaxRoundRange = 500
                    };

                    await _apiKeys.InsertOneAsync(toInsert);
                    update = Builders<AuthenticationRecordModel>.Update.Push(x => x.APIKeys, toInsert.Key);

                    if (update != null) await _users.FindOneAndUpdateAsync(filter, update);
                }
            }
            else
            {
                // No existing user, we must create a user record
                // Everyone gets a free API key
                var toInsert = new ApiKeyModel
                {
                    Name = user.PhpBBUsername,
                    MaxDocumentsReturned = 100000,
                    MaxParallelCalls = 2,
                    MaxRoundRange = 500
                };

                await _apiKeys.InsertOneAsync(toInsert);

                usersearch = new AuthenticationRecordModel
                {
                    PhpBBUsername = user.PhpBBUsername,
                    ByondKey = user.ByondKey,
                    ByondCKey = user.ByondCKey,
                    Roles = new List<string>
                    {
                        "User"
                    },
                    APIKeys = new List<ObjectId>
                    {
                        toInsert.Key
                    }
                };
                await _users.InsertOneAsync(usersearch);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.PhpBBUsername),
                new Claim("CKey", user.ByondCKey)
            };
            claims.AddRange(usersearch.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

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