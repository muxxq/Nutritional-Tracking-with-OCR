using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutriScan.Application.Auth.Dtos;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Auth.Commands.Refresh;

/// <summary>
/// Rotates a refresh token. Presenting a token that was already rotated means it was copied,
/// so the whole family (one login session) is revoked (RFC 9700 refresh token reuse detection).
/// </summary>
public sealed class RefreshCommandHandler(
    IApplicationDbContext db,
    ITokenService tokens,
    AuthSessionIssuer issuer,
    IDateTimeProvider clock,
    ILogger<RefreshCommandHandler> logger) : IRequestHandler<RefreshCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            throw new UnauthorizedException(AuthErrors.InvalidSession);
        }

        var now = clock.UtcNow;
        var hash = tokens.HashRefreshToken(request.RefreshToken);

        var current = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (current is null)
        {
            throw new UnauthorizedException(AuthErrors.InvalidSession);
        }

        if (current.IsRevoked)
        {
            await RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
            logger.LogWarning(
                "Refresh token reuse detected for user {UserId}, family {FamilyId}; family revoked",
                current.UserId, current.FamilyId);
            throw new UnauthorizedException(AuthErrors.InvalidSession);
        }

        if (current.IsExpired(now))
        {
            throw new UnauthorizedException(AuthErrors.InvalidSession);
        }

        var issued = issuer.Issue(current.User, current.FamilyId);
        current.Revoke(now, issued.RefreshToken.Id);
        await db.SaveChangesAsync(cancellationToken);

        return issued.Result;
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken cancellationToken)
    {
        var active = await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.Revoke(now);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
