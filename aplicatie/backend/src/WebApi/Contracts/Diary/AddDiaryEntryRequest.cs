using NutriScan.Domain.Enums;

namespace NutriScan.WebApi.Contracts.Diary;

/// <summary>A food (in grams or servings) or a recipe (in servings) eaten at a meal; give exactly one of the two ids.</summary>
public sealed record AddDiaryEntryRequest(
    Guid? FoodId, Guid? RecipeId, DateOnly Date, MealType MealType, double? QuantityGrams, double? Servings);
