using NutriScan.Domain.Enums;

namespace NutriScan.Application.Foods;

/// <summary>The editable fields of a food (values per 100 g), shared by create, update and, later, OCR validation.</summary>
public interface IFoodFields
{
    string Name { get; }
    string? Brand { get; }
    string? Barcode { get; }
    double? ServingSizeGrams { get; }
    double Calories { get; }
    double Proteins { get; }
    double Carbohydrates { get; }
    double? Sugars { get; }
    double Fats { get; }
    double? SaturatedFats { get; }
    double? Fiber { get; }
    double? SaltGrams { get; }

    /// <summary>Store aisle for the shopping list; null means <see cref="FoodCategory.Other"/>.</summary>
    FoodCategory? Category { get; }
}
