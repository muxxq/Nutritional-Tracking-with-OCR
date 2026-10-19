using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Planner.Dtos;

namespace NutriScan.Application.Planner.Queries.GetPlannedMeal;

public sealed class GetPlannedMealQueryHandler(IApplicationDbContext db) : IRequestHandler<GetPlannedMealQuery, PlannedMealDto>
{
    public async Task<PlannedMealDto> Handle(GetPlannedMealQuery request, CancellationToken cancellationToken) =>
        PlannedMealDto.From(await db.LoadOwnAsync(request.UserId, request.PlannedMealId, cancellationToken));
}
