using System;
using Microsoft.AspNetCore.Authentication;
using ScrubbyWeb.Services.Mongo;

namespace ScrubbyWeb.Services
{
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAPIKeySupport(this AuthenticationBuilder authenticationBuilder,
            Action<APIKeyAuthenticationOptions> options)
        {
            return authenticationBuilder.AddScheme<APIKeyAuthenticationOptions, MongoAPIKeyAuthenticationHandler>(
                APIKeyAuthenticationOptions.DefaultScheme, options);
        }
    }
}