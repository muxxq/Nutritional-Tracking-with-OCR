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
    // Concurrent logins for the same account conflict on the failure counter; re-read and re-apply.
    private const int MaxSaveAttempts = 3;

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        var now = clock.UtcNow;
        PasswordCheckResult? check = null;

        for (var attempt = 1; attempt <= MaxSaveAttempts; attempt++)
        {
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

            // The password is verified once; a retry only re-applies the outcome to fresh data.
            check ??= hasher.Verify(user.PasswordHash, request.Password);

            AuthResult? result = null;
            if (check == PasswordCheckResult.Failed)
            {
                user.RegisterFailedLogin(now);
            }
            else
            {
                if (check == PasswordCheckResult.SuccessRehashNeeded)
                {
                    user.PasswordHash = hasher.Hash(request.Password);
                }

                user.RegisterSuccessfulLogin();
                result = issuer.Issue(user, familyId: Guid.NewGuid()).Result;
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ChangeTracker.Clear();
                continue;
            }

            return result ?? throw new UnauthorizedException(AuthErrors.InvalidCredentials);
        }

        // Fail closed when the account is under heavy concurrent attack.
        throw new UnauthorizedException(AuthErrors.InvalidCredentials);
    }
}
