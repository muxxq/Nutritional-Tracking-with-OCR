using FluentValidation;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Recipes;

/// <summary>
/// Rules that need no database. Whether each ingredient's food is visible to the user is checked by the handlers (404).
/// </summary>
public sealed class RecipeFieldsValidator : AbstractValidator<IRecipeFields>
{
    public const int MaxIngredients = 50;
    public const int MaxPrepTimeMinutes = 1440;

    public RecipeFieldsValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Description).MaximumLength(1000);
        RuleFor(r => r.Instructions).MaximumLength(10_000);
        RuleFor(r => r.ServingsCount).InclusiveBetween(1, Recipe.MaxServings);
        RuleFor(r => r.PrepTimeMinutes).InclusiveBetween(0, MaxPrepTimeMinutes).When(r => r.PrepTimeMinutes is not null);

        RuleFor(r => r.Ingredients)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Rețeta trebuie să aibă cel puțin un ingredient.")
            .Must(list => list.Count <= MaxIngredients).WithMessage($"Rețeta poate avea cel mult {MaxIngredients} de ingrediente.")
            .Must(list => list.Where(i => i is not null).GroupBy(i => i.FoodId).All(g => g.Count() == 1))
            .WithMessage(RecipeErrors.DuplicateIngredient);
        RuleForEach(r => r.Ingredients)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .ChildRules(ingredient =>
            {
                ingredient.RuleFor(i => i.FoodId).NotEmpty();
                ingredient.RuleFor(i => i.QuantityGrams).GreaterThan(0).LessThanOrEqualTo(DiaryQuantity.MaxGrams);
            });
    }
}
