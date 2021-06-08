using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ScrubbyWeb.Services
{
    public abstract class APIKeyAuthenticationHandlerBase : AuthenticationHandler<APIKeyAuthenticationOptions>
    {
        private const string ProblemDetailsContentType = "application/problem+json";
        protected const string APIKeyHeader = "X-Scrubby-API-Key";

        public APIKeyAuthenticationHandlerBase(IOptionsMonitor<APIKeyAuthenticationOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) :
            base(options, logger, encoder, clock)
        {
            
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            Response.ContentType = ProblemDetailsContentType;

            await Response.WriteAsync(
                "Oopsie woopsie, looks wike you are unawfenticated! uwu Pwease cweate an account on Scwubby to get an API key >w<");
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            Response.ContentType = ProblemDetailsContentType;

            await Response.WriteAsync(
                "Oopsie woopsie, looks wike you dont have access hewe! ^w^ Pwease contact bobbahbrown if this is in ewwow >w<");
        }
    }
}