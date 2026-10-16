using NutriScan.Application.Auth.Dtos;

namespace NutriScan.WebApi.Contracts.Auth;

/// <summary>Response body for successful authentication. The refresh token travels only in the HttpOnly cookie.</summary>
public sealed record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, UserDto User)
{
    public static AuthResponse From(AuthResult result) =>
        new(result.AccessToken, result.AccessTokenExpiresAtUtc, result.User);
}
