using FluentValidation;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Diary.Commands.AddDiaryEntry;

public sealed class AddDiaryEntryCommandValidator : AbstractValidator<AddDiaryEntryCommand>
{
    public AddDiaryEntryCommandValidator(IDateTimeProvider clock)
    {
        Include(new DiaryEntryFieldsValidator(clock));

        RuleFor(c => c)
            .Must(c => c.FoodId.HasValue != c.RecipeId.HasValue)
            .OverridePropertyName("Item")
            .WithMessage(DiaryErrors.ExactlyOneItem);
        // With grams forbidden, the shared "exactly one quantity" rule makes servings required for a recipe.
        RuleFor(c => c.QuantityGrams)
            .Null().When(c => c.RecipeId is not null)
            .OverridePropertyName("Quantity")
            .WithMessage(DiaryErrors.RecipeServingsOnly);
    }
}
