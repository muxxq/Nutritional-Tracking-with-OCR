using NutriScan.Domain.Entities;

namespace NutriScan.Domain.Nutrition;

public sealed record ShoppingLine(FoodItem Food, double Grams);

/// <summary>
/// FR-12: what to buy for a set of planned meals. A planned food counts with its grams; a planned recipe with each
/// ingredient scaled to the planned servings (grams × planned / recipe servings). Identical foods are summed (ADR-008 D3).
/// </summary>
public static class ShoppingListBuilder
{
    /// <summary>Meals must be loaded with their food, or their recipe and its ingredients' foods. Grams are unrounded.</summary>
    public static IReadOnlyList<ShoppingLine> Build(IEnumerable<PlannedMeal> meals) =>
        meals
            .SelectMany(Lines)
            .GroupBy(line => line.Food.Id)
            .Select(group => new ShoppingLine(group.First().Food, group.Sum(line => line.Grams)))
            .ToList();

    private static IEnumerable<ShoppingLine> Lines(PlannedMeal meal) =>
        meal.IsRecipe
            ? meal.Recipe!.Ingredients.Select(i =>
                new ShoppingLine(i.FoodItem, i.QuantityGrams * meal.Servings!.Value / meal.Recipe.ServingsCount))
            : [new ShoppingLine(meal.FoodItem!, meal.QuantityGrams!.Value)];
}
