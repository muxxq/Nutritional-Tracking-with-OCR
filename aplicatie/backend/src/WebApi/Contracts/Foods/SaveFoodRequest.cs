using NutriScan.Domain.Enums;

namespace NutriScan.WebApi.Contracts.Foods;

/// <summary>Food fields for create and update; nutrition values are per 100 g.</summary>
public sealed record SaveFoodRequest(
    string Name, string? Brand, string? Barcode, double? ServingSizeGrams,
    double Calories, double Proteins, double Carbohydrates, double? Sugars,
    double Fats, double? SaturatedFats, double? Fiber, double? SaltGrams,
    FoodCategory? Category = null);
