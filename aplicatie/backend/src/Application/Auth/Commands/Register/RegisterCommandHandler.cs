using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Auth.Dtos;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher hasher,
    AuthSessionIssuer issuer) : IRequestHandler<RegisterCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new ConflictException(AuthErrors.EmailTaken);
        }

        var user = new User
        {
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim()
        };
        db.Users.Add(user);

        var session = issuer.Issue(user, familyId: Guid.NewGuid());

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two concurrent registrations passed the check above; the unique index rejected the second.
            throw new ConflictException(AuthErrors.EmailTaken);
        }

        return session.Result;
    }
}
