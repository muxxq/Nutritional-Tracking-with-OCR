using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Profile.Dtos;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<UpdateProfileCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await db.LoadUserAsync(request.UserId, cancellationToken);
        var today = DateOnly.FromDateTime(clock.UtcNow);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.BirthDate = request.BirthDate;
        user.HeightCm = request.HeightCm;
        user.WeightKg = request.WeightKg;
        user.Gender = request.Gender;
        user.ActivityLevel = request.ActivityLevel;
        user.Goal = request.Goal;

        // The real profile wins: overrides that no longer fit it fall back to calculated targets.
        if (!NutritionTargetCalculator.Calculate(user, today).IsValid)
        {
            user.CaloriesOverride = null;
            user.ProteinsGramsOverride = null;
            user.CarbsGramsOverride = null;
            user.FatsGramsOverride = null;
        }

        var targets = ProfileLoader.CalculateTargets(user, today);
        await db.SaveChangesAsync(cancellationToken);

        return ProfileDto.From(user, targets, today);
    }
}
