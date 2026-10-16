using NutriScan.Application.Auth.Dtos;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.Auth;

public sealed record IssuedSession(AuthResult Result, RefreshToken RefreshToken);

/// <summary>
/// Issues an access token and a new refresh token in the given family.
/// Only the refresh token hash is tracked for persistence; the caller saves changes.
/// </summary>
public sealed class AuthSessionIssuer(IApplicationDbContext db, ITokenService tokens, IDateTimeProvider clock)
{
    public IssuedSession Issue(User user, Guid familyId)
    {
        var accessToken = tokens.CreateAccessToken(user);
        var refreshToken = tokens.GenerateRefreshToken();

        var entity = RefreshToken.Create(
            user.Id,
            tokens.HashRefreshToken(refreshToken),
            familyId,
            clock.UtcNow,
            tokens.RefreshTokenLifetime);
        db.RefreshTokens.Add(entity);

        var result = new AuthResult(accessToken.Token, accessToken.ExpiresAtUtc, refreshToken, UserDto.From(user));
        return new IssuedSession(result, entity);
    }
}
