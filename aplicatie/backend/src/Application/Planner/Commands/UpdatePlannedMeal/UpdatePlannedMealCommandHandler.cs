using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Planner.Dtos;

namespace NutriScan.Application.Planner.Commands.UpdatePlannedMeal;

/// <summary>A diary entry already created from this meal keeps its own snapshot; editing the plan doesn't touch it.</summary>
public sealed class UpdatePlannedMealCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<UpdatePlannedMealCommand, PlannedMealDto>
{
    public async Task<PlannedMealDto> Handle(UpdatePlannedMealCommand request, CancellationToken cancellationToken)
    {
        // An archived food or recipe still backs the meals planned before it was archived (ADR-007 D7).
        var meal = await db.LoadOwnAsync(request.UserId, request.PlannedMealId, cancellationToken);
        meal.Apply(request);
        meal.UpdatedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return PlannedMealDto.From(meal);
    }
}
