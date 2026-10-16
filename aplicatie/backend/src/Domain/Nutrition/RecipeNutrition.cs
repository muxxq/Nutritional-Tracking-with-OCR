using NutriScan.Domain.Entities;

namespace NutriScan.Domain.Nutrition;

public sealed record RecipeNutritionResult(IReadOnlyList<Macros> Ingredients, Macros Total, Macros PerServing);

/// <summary>
/// FR-09: a recipe's totals and per-serving values, computed from the ingredients' current foods.
/// Nothing is stored on the recipe (ADR-006 D1); diary entries keep their own snapshot.
/// </summary>
public static class RecipeNutrition
{
    /// <summary>The ingredients must be loaded with their <see cref="RecipeIngredient.FoodItem"/>.</summary>
    public static RecipeNutritionResult Of(Recipe recipe)
    {
        var ingredients = recipe.Ingredients.Select(i => Macros.Of(i.FoodItem, i.QuantityGrams)).ToList();
        var total = ingredients.Aggregate(Macros.Zero, (sum, m) => sum + m);
        return new RecipeNutritionResult(ingredients, total, total / recipe.ServingsCount);
    }
}
