using NutriScan.Domain.Enums;

namespace NutriScan.WebApi.Contracts.Diary;

/// <summary>New quantity, meal and date of an entry; the food cannot change.</summary>
public sealed record UpdateDiaryEntryRequest(DateOnly Date, MealType MealType, double? QuantityGrams, double? Servings);
