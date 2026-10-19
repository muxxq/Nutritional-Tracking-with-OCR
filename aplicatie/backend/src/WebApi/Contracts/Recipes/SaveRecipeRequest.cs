using NutriScan.Application.Recipes;

namespace NutriScan.WebApi.Contracts.Recipes;

/// <summary>A whole recipe: the ingredient list replaces the existing one on update.</summary>
public sealed record SaveRecipeRequest(
    string Name, string? Description, string? Instructions, int ServingsCount, int? PrepTimeMinutes,
    IReadOnlyList<RecipeIngredientInput> Ingredients);
