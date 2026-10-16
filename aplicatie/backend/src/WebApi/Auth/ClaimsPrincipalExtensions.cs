using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace NutriScan.WebApi.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's id from the access token's "sub" claim; never taken from the request.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;
}
