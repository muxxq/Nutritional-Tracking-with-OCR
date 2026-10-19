using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Planner.Commands.DeletePlannedMeal;

/// <summary>Physical delete: nothing refers to a planned meal. A diary entry logged from it stays (ADR-007 D8).</summary>
public sealed class DeletePlannedMealCommandHandler(IApplicationDbContext db) : IRequestHandler<DeletePlannedMealCommand>
{
    public async Task Handle(DeletePlannedMealCommand request, CancellationToken cancellationToken)
    {
        var meal = await db.PlannedMeals
            .SingleOrDefaultAsync(m => m.Id == request.PlannedMealId && m.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException(PlannerErrors.NotFound);

        db.PlannedMeals.Remove(meal);
        await db.SaveChangesAsync(cancellationToken);
    }
}
