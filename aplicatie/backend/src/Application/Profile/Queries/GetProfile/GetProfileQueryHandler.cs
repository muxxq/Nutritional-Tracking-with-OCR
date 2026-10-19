using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Profile.Dtos;

namespace NutriScan.Application.Profile.Queries.GetProfile;

public sealed class GetProfileQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<GetProfileQuery, ProfileDto>
{
    public async Task<ProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await db.LoadUserAsync(request.UserId, cancellationToken);
        var today = DateOnly.FromDateTime(clock.UtcNow);
        return ProfileDto.From(user, ProfileLoader.CalculateTargets(user, today), today);
    }
}
