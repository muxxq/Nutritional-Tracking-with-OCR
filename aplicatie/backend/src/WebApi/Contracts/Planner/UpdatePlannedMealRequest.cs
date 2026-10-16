using NutriScan.Domain.Enums;

namespace NutriScan.WebApi.Contracts.Planner;

/// <summary>New date, meal and quantity of a planned meal; the food or recipe cannot change.</summary>
public sealed record UpdatePlannedMealRequest(DateOnly Date, MealType MealType, double? QuantityGrams, double? Servings);
