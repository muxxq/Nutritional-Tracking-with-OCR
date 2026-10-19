using MediatR;
using NutriScan.Application.Planner.Dtos;

namespace NutriScan.Application.Planner.Queries.GetPlannedMeal;

public sealed record GetPlannedMealQuery(Guid UserId, Guid PlannedMealId) : IRequest<PlannedMealDto>;
