using FluentValidation;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Diary;

/// <summary>
/// Rules that need no database. Whether a food has a serving size is checked by the handlers,
/// through <see cref="DiaryQuantity.Resolve"/>.
/// </summary>
public sealed class DiaryEntryFieldsValidator : AbstractValidator<IDiaryEntryFields>
{
    private static readonly DateOnly EarliestDate = new(2000, 1, 1);

    public DiaryEntryFieldsValidator(IDateTimeProvider clock)
    {
        // One day of slack: the server doesn't know the client's time zone, and "today" east of UTC is tomorrow here.
        RuleFor(e => e.Date)
            .Must(date => date >= EarliestDate && date <= DateOnly.FromDateTime(clock.UtcNow).AddDays(1))
            .WithMessage("Data trebuie să fie între 01.01.2000 și ziua de mâine.");
        RuleFor(e => e.MealType).IsInEnum();

        RuleFor(e => e)
            .Must(e => e.QuantityGrams.HasValue != e.Servings.HasValue)
            .OverridePropertyName("Quantity")
            .WithMessage(DiaryQuantity.ExactlyOneQuantity);
        RuleFor(e => e.QuantityGrams)
            .GreaterThan(0).LessThanOrEqualTo(DiaryQuantity.MaxGrams)
            .When(e => e.QuantityGrams is not null);
        RuleFor(e => e.Servings)
            .GreaterThan(0).LessThanOrEqualTo(DiaryQuantity.MaxServings)
            .When(e => e.Servings is not null);
    }
}
