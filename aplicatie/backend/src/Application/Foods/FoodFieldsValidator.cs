using FluentValidation;
using NutriScan.Domain.Common;

namespace NutriScan.Application.Foods;

/// <summary>
/// Physically possible nutrition values per 100 g. Rules such as "sugars ≤ carbohydrates" also catch
/// typical OCR misreads, so this validator will check scan results too.
/// </summary>
public sealed class FoodFieldsValidator : AbstractValidator<IFoodFields>
{
    public FoodFieldsValidator()
    {
        RuleFor(f => f.Name)
            .NotEmpty()
            .MaximumLength(250)
            .Must(name => TextNormalizer.ForSearch(name ?? string.Empty).Length > 0)
            .WithMessage("Numele trebuie să conțină litere sau cifre.");
        RuleFor(f => f.Brand).MaximumLength(150);
        RuleFor(f => f.Barcode)
            .Matches(@"^\d{8,14}$").When(f => f.Barcode is not null)
            .WithMessage("Codul de bare trebuie să aibă între 8 și 14 cifre.");
        RuleFor(f => f.ServingSizeGrams).InclusiveBetween(1, 2000).When(f => f.ServingSizeGrams is not null);

        // Pure fat has 900 kcal per 100 g; nothing can have more.
        RuleFor(f => f.Calories).InclusiveBetween(0, 900);
        RuleFor(f => f.Proteins).InclusiveBetween(0, 100);
        RuleFor(f => f.Carbohydrates).InclusiveBetween(0, 100);
        RuleFor(f => f.Fats).InclusiveBetween(0, 100);
        RuleFor(f => f.Proteins + f.Carbohydrates + f.Fats)
            .LessThanOrEqualTo(100)
            .OverridePropertyName("Macronutrients")
            .WithMessage("Proteinele, carbohidrații și grăsimile nu pot depăși împreună 100 g la 100 g de produs.");

        RuleFor(f => f.Sugars)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(f => f.Carbohydrates).WithMessage("Zaharurile nu pot depăși carbohidrații.")
            .When(f => f.Sugars is not null);
        RuleFor(f => f.SaturatedFats)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(f => f.Fats).WithMessage("Grăsimile saturate nu pot depăși grăsimile totale.")
            .When(f => f.SaturatedFats is not null);
        RuleFor(f => f.Fiber).InclusiveBetween(0, 100).When(f => f.Fiber is not null);
        RuleFor(f => f.SaltGrams).InclusiveBetween(0, 100).When(f => f.SaltGrams is not null);
        RuleFor(f => f.Category).IsInEnum().When(f => f.Category is not null);
    }
}
