using NutriScan.Domain.Enums;

namespace NutriScan.Application.Diary;

/// <summary>Fields shared by adding and editing a diary entry.</summary>
public interface IDiaryEntryFields
{
    DateOnly Date { get; }
    MealType MealType { get; }
    double? QuantityGrams { get; }
    double? Servings { get; }
}
