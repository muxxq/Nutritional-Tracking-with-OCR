using NutriScan.Domain.Enums;

namespace NutriScan.Application.Planner;

/// <summary>Fields shared by planning and editing a meal; the food or recipe itself never changes (ADR-007 D8).</summary>
public interface IPlannedMealFields
{
    DateOnly Date { get; }
    MealType MealType { get; }
    double? QuantityGrams { get; }
    double? Servings { get; }
}
