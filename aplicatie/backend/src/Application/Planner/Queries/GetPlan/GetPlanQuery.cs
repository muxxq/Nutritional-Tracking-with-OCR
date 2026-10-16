using MediatR;
using NutriScan.Application.Planner.Dtos;

namespace NutriScan.Application.Planner.Queries.GetPlan;

/// <summary>Read model for the planner: every day of the range with its four meals, totals and status against the target.</summary>
public sealed record GetPlanQuery(Guid UserId, DateOnly From, DateOnly To) : IRequest<PlanDto>;
