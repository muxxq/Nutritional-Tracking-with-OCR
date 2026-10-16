using FluentValidation;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Planner.Queries.GetPlan;

public sealed class GetPlanQueryValidator : AbstractValidator<GetPlanQuery>
{
    /// <summary>A month: enough for a week view and for a shopping list over a range (FR-12).</summary>
    public const int MaxDays = 31;

    public GetPlanQueryValidator(IDateTimeProvider clock)
    {
        var latest = () => DateOnly.FromDateTime(clock.UtcNow).AddDays(PlannedMealFieldsValidator.MaxDaysAhead);

        RuleFor(q => q.From)
            .Must(d => d >= PlannedMealFieldsValidator.EarliestDate && d <= latest())
            .WithMessage(PlannerErrors.DateRange);
        RuleFor(q => q.To)
            .Must(d => d >= PlannedMealFieldsValidator.EarliestDate && d <= latest())
            .WithMessage(PlannerErrors.DateRange);
        RuleFor(q => q)
            .Must(q => q.From <= q.To && q.To.DayNumber - q.From.DayNumber + 1 <= MaxDays)
            .OverridePropertyName("Range")
            .WithMessage($"Intervalul trebuie să înceapă înainte să se termine și să aibă cel mult {MaxDays} de zile.");
    }
}
