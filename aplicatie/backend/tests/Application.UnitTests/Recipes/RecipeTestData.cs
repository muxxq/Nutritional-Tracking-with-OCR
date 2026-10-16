using NutriScan.Application.Recipes;
using NutriScan.Application.Recipes.Commands.CreateRecipe;
using NutriScan.Application.Recipes.Commands.UpdateRecipe;

namespace NutriScan.Application.UnitTests.Recipes;

public static class RecipeTestData
{
    /// <summary>The SRS example (FR-09): 200 g oats, 4 servings → 194.5 kcal per serving.</summary>
    public static CreateRecipeCommand Create(Guid userId, params (Guid FoodId, double Grams)[] ingredients) => new(
        userId, "  Terci de ovăz  ", "Mic dejun rapid", "Fierbe ovăzul în apă.", 4, 10,
        ingredients.Select(i => new RecipeIngredientInput(i.FoodId, i.Grams)).ToList());

    public static UpdateRecipeCommand Update(Guid userId, Guid recipeId, CreateRecipeCommand c) =>
        UpdateRecipeCommand.From(recipeId, c) with { UserId = userId };
}
