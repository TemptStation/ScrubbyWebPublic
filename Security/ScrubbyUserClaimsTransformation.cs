using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using ScrubbyWeb.Services.Interfaces;

namespace ScrubbyWeb.Security
{
    public class ScrubbyUserClaimsTransformation : IClaimsTransformation
    {
        private readonly IUserService _userService;

        public ScrubbyUserClaimsTransformation(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is null or { IsAuthenticated: false })
                return principal;

            var user = await _userService.GetUser(principal.Identity.Name);
            if (user != null)
            {
                ((ClaimsIdentity)principal.Identity).AddClaims(user.Roles.Select(x => new Claim(ClaimTypes.Role, x)));
            }

            return principal;
        }
    }
}