using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Auth.Commands.Logout;

/// <summary>
/// Ends the session on the current device only, by revoking its token family. Idempotent.
/// </summary>
public sealed class LogoutCommandHandler(
    IApplicationDbContext db,
    ITokenService tokens,
    IDateTimeProvider clock) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return;
        }

        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var familyId = await db.RefreshTokens
            .Where(t => t.TokenHash == hash)
            .Select(t => (Guid?)t.FamilyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (familyId is null)
        {
            return;
        }

        var now = clock.UtcNow;
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
