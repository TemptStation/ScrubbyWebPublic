using Microsoft.AspNetCore.Authentication;

namespace ScrubbyWeb.Services
{
    public class APIKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "API Key";
        public static string Scheme => DefaultScheme;
        public static string AuthenticationType => DefaultScheme;
    }
}