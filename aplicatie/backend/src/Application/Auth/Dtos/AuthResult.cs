namespace NutriScan.Application.Auth.Dtos;

/// <summary>
/// Outcome of a successful authentication. <see cref="RefreshToken"/> is the plain value,
/// handed to the API layer only so it can be written into the HttpOnly cookie.
/// </summary>
public sealed record AuthResult(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, UserDto User);
