using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrubbyWeb.Models;

namespace ScrubbyWeb.Services.Mongo
{
    public class MongoAPIKeyAuthenticationHandler : APIKeyAuthenticationHandlerBase
    {
        private readonly IMongoCollection<ApiKeyModel> _apiKeys;
        private readonly IMongoCollection<AuthenticationRecordModel> _users;
    
        public MongoAPIKeyAuthenticationHandler(MongoAccess client, IOptionsMonitor<APIKeyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
            _apiKeys = client.DB.GetCollection<ApiKeyModel>("scrubby_api_keys");
            _users = client.DB.GetCollection<AuthenticationRecordModel>("scrubby_users");
        }
        
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(APIKeyHeader, out var apiKeyHeaderValues))
                return AuthenticateResult.NoResult();

            if (apiKeyHeaderValues.Count == 0 || !ObjectId.TryParse(apiKeyHeaderValues.First(), out var apiKey))
                return AuthenticateResult.NoResult();

            var apiKeyRecord = await _apiKeys.Find(x => x.Key == apiKey).Limit(1).FirstOrDefaultAsync();

            if (apiKeyRecord != null)
            {
                var user = await _users.Find(x => x.APIKeys.Contains(apiKey)).Limit(1).FirstOrDefaultAsync();

                if (user == null) return AuthenticateResult.Fail("API key not owned by user, contact bobbahbrown");

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.PhpBBUsername),
                    new Claim("CKey", user.ByondCKey),
                    new Claim("API Key", apiKeyHeaderValues.First())
                };

                foreach (var role in user.Roles) claims.Add(new Claim(ClaimTypes.Role, role));

                var identity = new ClaimsIdentity(claims, APIKeyAuthenticationOptions.AuthenticationType);
                var identities = new List<ClaimsIdentity> {identity};
                var principal = new ClaimsPrincipal(identities);
                var ticket = new AuthenticationTicket(principal, APIKeyAuthenticationOptions.Scheme);

                return AuthenticateResult.Success(ticket);
            }

            return AuthenticateResult.Fail("Invalid API key provided.");
        }
    }
}