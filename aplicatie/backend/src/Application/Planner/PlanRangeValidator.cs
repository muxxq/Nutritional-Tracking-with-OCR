using FluentValidation;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Planner;

/// <summary>A range of plan days: the plan view and the shopping list (FR-12) both read one.</summary>
public interface IPlanRange
{
    DateOnly From { get; }
    DateOnly To { get; }
}

public sealed class PlanRangeValidator : AbstractValidator<IPlanRange>
{
    /// <summary>A month: enough for a week view and for a shopping list over a range (FR-12).</summary>
    public const int MaxDays = 31;

    public PlanRangeValidator(IDateTimeProvider clock)
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
            .WithMessage(PlannerErrors.RangeLimits);
    }
}
