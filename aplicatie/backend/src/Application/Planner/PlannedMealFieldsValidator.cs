using FluentValidation;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Planner;

/// <summary>
/// Rules that need no database. A food's serving size and the kind of an existing planned meal are checked by the handlers.
/// </summary>
public sealed class PlannedMealFieldsValidator : AbstractValidator<IPlannedMealFields>
{
    public static readonly DateOnly EarliestDate = new(2000, 1, 1);
    public const int MaxDaysAhead = 366;

    public PlannedMealFieldsValidator(IDateTimeProvider clock)
    {
        RuleFor(m => m.Date)
            .Must(date => date >= EarliestDate && date <= DateOnly.FromDateTime(clock.UtcNow).AddDays(MaxDaysAhead))
            .WithMessage(PlannerErrors.DateRange);
        RuleFor(m => m.MealType).IsInEnum();

        RuleFor(m => m)
            .Must(m => m.QuantityGrams.HasValue != m.Servings.HasValue)
            .OverridePropertyName("Quantity")
            .WithMessage(DiaryQuantity.ExactlyOneQuantity);
        RuleFor(m => m.QuantityGrams)
            .GreaterThan(0).LessThanOrEqualTo(DiaryQuantity.MaxGrams)
            .When(m => m.QuantityGrams is not null);
        RuleFor(m => m.Servings)
            .GreaterThan(0).LessThanOrEqualTo(DiaryQuantity.MaxServings)
            .When(m => m.Servings is not null);
    }
}
