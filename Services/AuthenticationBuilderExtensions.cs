using System;
using Microsoft.AspNetCore.Authentication;

namespace ScrubbyWeb.Services
{
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAPIKeySupport(this AuthenticationBuilder authenticationBuilder,
            Action<APIKeyAuthenticationOptions> options)
        {
            return authenticationBuilder.AddScheme<APIKeyAuthenticationOptions, APIKeyAuthenticationHandler>(
                APIKeyAuthenticationOptions.DefaultScheme, options);
        }
    }
}