using NutriScan.Domain.Entities;

namespace NutriScan.Domain.UnitTests.Nutrition;

internal static class RecipeTestData
{
    /// <summary>389 kcal, 16.9 P, 66.3 C, 6.9 F per 100 g.</summary>
    public static FoodItem Oats() => new() { Name = "Fulgi de ovăz", Calories = 389, Proteins = 16.9, Carbohydrates = 66.3, Fats = 6.9 };

    /// <summary>120 kcal, 22.5 P, 0 C, 2.6 F per 100 g.</summary>
    public static FoodItem Chicken() => new() { Name = "Piept de pui", Calories = 120, Proteins = 22.5, Carbohydrates = 0, Fats = 2.6 };

    public static Recipe Recipe(int servings, params (FoodItem Food, double Grams)[] ingredients)
    {
        var recipe = new Recipe { Name = "Rețetă", ServingsCount = servings };
        foreach (var (food, grams) in ingredients)
        {
            recipe.Ingredients.Add(new RecipeIngredient { FoodItem = food, FoodItemId = food.Id, QuantityGrams = grams });
        }

        return recipe;
    }

    /// <summary>FR-09 acceptance example: 200 g oats, 4 servings → 194.5 kcal per serving.</summary>
    public static Recipe SrsRecipe() => Recipe(4, (Oats(), 200));
}
