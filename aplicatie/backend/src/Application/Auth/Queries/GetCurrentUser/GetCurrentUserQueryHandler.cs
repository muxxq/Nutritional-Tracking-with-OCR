using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Auth.Dtos;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IApplicationDbContext db) : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        // A valid token for a deleted account is no longer a valid session.
        return user is null
            ? throw new UnauthorizedException(AuthErrors.InvalidSession)
            : UserDto.From(user);
    }
}
