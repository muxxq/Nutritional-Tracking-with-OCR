using NutriScan.Domain.Enums;

namespace NutriScan.WebApi.Contracts.Diary;

/// <summary>A food eaten at a meal; give either grams or servings.</summary>
public sealed record AddDiaryEntryRequest(Guid FoodId, DateOnly Date, MealType MealType, double? QuantityGrams, double? Servings);
