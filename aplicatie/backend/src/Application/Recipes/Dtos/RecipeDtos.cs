using NutriScan.Application.Common.Models;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Recipes.Dtos;

public sealed record RecipeFoodDto(Guid Id, string Name, string? Brand, bool IsArchived)
{
    public static RecipeFoodDto From(FoodItem f) => new(f.Id, f.Name, f.Brand, f.ArchivedAtUtc is not null);
}

public sealed record RecipeIngredientDto(RecipeFoodDto Food, double QuantityGrams, NutrientsDto Nutrients);

public sealed record RecipeDto(
    Guid Id, string Name, string? Description, string? Instructions, int ServingsCount, int? PrepTimeMinutes,
    bool IsArchived, IReadOnlyList<RecipeIngredientDto> Ingredients, NutrientsDto Total, NutrientsDto PerServing)
{
    /// <summary>The recipe must be loaded with its ingredients' foods. Per-serving values come from the unrounded total.</summary>
    public static RecipeDto From(Recipe recipe)
    {
        var nutrition = RecipeNutrition.Of(recipe);
        var ingredients = recipe.Ingredients
            .Zip(nutrition.Ingredients, (i, m) =>
                new RecipeIngredientDto(RecipeFoodDto.From(i.FoodItem), i.QuantityGrams, NutrientsDto.From(m).Rounded()))
            .ToList();

        return new RecipeDto(
            recipe.Id, recipe.Name, recipe.Description, recipe.Instructions, recipe.ServingsCount, recipe.PrepTimeMinutes,
            recipe.ArchivedAtUtc is not null, ingredients,
            NutrientsDto.From(nutrition.Total).Rounded(), NutrientsDto.From(nutrition.PerServing).Rounded());
    }
}

public sealed record RecipeSummaryDto(
    Guid Id, string Name, int ServingsCount, int? PrepTimeMinutes, int IngredientCount, NutrientsDto PerServing);
