using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Auth.Dtos;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Common.Models;

namespace NutriScan.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher hasher,
    AuthSessionIssuer issuer,
    IDateTimeProvider clock) : IRequestHandler<LoginCommand, AuthResult>
{
    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        var now = clock.UtcNow;

        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            // Spend the same hashing time as a real check so response timing does not reveal registered emails.
            hasher.Verify(hasher.DummyHash, request.Password);
            throw new UnauthorizedException(AuthErrors.InvalidCredentials);
        }

        if (user.IsLockedOut(now))
        {
            // Not the real hash: a locked account must not be probed, but the response time must stay the same.
            hasher.Verify(hasher.DummyHash, request.Password);
            throw new UnauthorizedException(AuthErrors.InvalidCredentials);
        }

        var check = hasher.Verify(user.PasswordHash, request.Password);
        if (check == PasswordCheckResult.Failed)
        {
            user.RegisterFailedLogin(now);
            await db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException(AuthErrors.InvalidCredentials);
        }

        if (check == PasswordCheckResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.Hash(request.Password);
        }

        user.RegisterSuccessfulLogin();
        var session = issuer.Issue(user, familyId: Guid.NewGuid());
        await db.SaveChangesAsync(cancellationToken);

        return session.Result;
    }
}
