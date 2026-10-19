using NutriScan.Domain.Enums;

namespace NutriScan.WebApi.Contracts.Planner;

/// <summary>A food (in grams or servings) or a recipe (in servings) planned for a meal; give exactly one of the two ids.</summary>
public sealed record CreatePlannedMealRequest(
    Guid? FoodId, Guid? RecipeId, DateOnly Date, MealType MealType, double? QuantityGrams, double? Servings);
