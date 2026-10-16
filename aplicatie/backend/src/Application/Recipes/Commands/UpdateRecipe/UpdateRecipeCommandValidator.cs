using FluentValidation;

namespace NutriScan.Application.Recipes.Commands.UpdateRecipe;

public sealed class UpdateRecipeCommandValidator : AbstractValidator<UpdateRecipeCommand>
{
    public UpdateRecipeCommandValidator() => Include(new RecipeFieldsValidator());
}
