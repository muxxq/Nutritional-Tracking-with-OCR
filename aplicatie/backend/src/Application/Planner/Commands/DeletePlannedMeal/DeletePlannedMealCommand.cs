using MediatR;

namespace NutriScan.Application.Planner.Commands.DeletePlannedMeal;

public sealed record DeletePlannedMealCommand(Guid UserId, Guid PlannedMealId) : IRequest;
