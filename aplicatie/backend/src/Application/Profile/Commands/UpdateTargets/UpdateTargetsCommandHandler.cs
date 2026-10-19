using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Profile.Dtos;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Profile.Commands.UpdateTargets;

public sealed class UpdateTargetsCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<UpdateTargetsCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(UpdateTargetsCommand request, CancellationToken cancellationToken)
    {
        var user = await db.LoadUserAsync(request.UserId, cancellationToken);
        var today = DateOnly.FromDateTime(clock.UtcNow);

        user.CaloriesOverride = request.Calories;
        user.ProteinsGramsOverride = request.ProteinsGrams;
        user.CarbsGramsOverride = request.CarbsGrams;
        user.FatsGramsOverride = request.FatsGrams;

        var result = NutritionTargetCalculator.Calculate(user, today);
        if (!result.IsValid)
        {
            // Nothing is saved: the tracked changes are discarded with the request scope.
            throw new ValidationException(new[] { new ValidationFailure("Targets", result.Error) });
        }

        await db.SaveChangesAsync(cancellationToken);
        return ProfileDto.From(user, result.Targets!, today);
    }
}
