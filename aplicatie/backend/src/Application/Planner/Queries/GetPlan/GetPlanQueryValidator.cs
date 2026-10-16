using FluentValidation;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Planner.Queries.GetPlan;

public sealed class GetPlanQueryValidator : AbstractValidator<GetPlanQuery>
{
    public GetPlanQueryValidator(IDateTimeProvider clock) => Include(new PlanRangeValidator(clock));
}
