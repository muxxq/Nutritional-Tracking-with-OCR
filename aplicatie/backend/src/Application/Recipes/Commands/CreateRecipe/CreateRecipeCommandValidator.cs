using FluentValidation;

namespace NutriScan.Application.Recipes.Commands.CreateRecipe;

public sealed class CreateRecipeCommandValidator : AbstractValidator<CreateRecipeCommand>
{
    public CreateRecipeCommandValidator() => Include(new RecipeFieldsValidator());
}
