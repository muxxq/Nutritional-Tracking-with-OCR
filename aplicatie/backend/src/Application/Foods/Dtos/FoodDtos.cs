using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.Foods.Dtos;

public sealed record Per100gDto(
    double Calories, double Proteins, double Carbohydrates, double? Sugars,
    double Fats, double? SaturatedFats, double? Fiber, double? SaltGrams);

public sealed record FoodDto(
    Guid Id, string Name, string? Brand, string? Barcode, double? ServingSizeGrams,
    Per100gDto Per100g, bool IsVerified, bool IsOwn, string? EnergyWarning, FoodCategory Category)
{
    public static FoodDto From(FoodItem f, Guid userId) => new(
        f.Id, f.Name, f.Brand, f.Barcode, f.ServingSizeGrams,
        new Per100gDto(f.Calories, f.Proteins, f.Carbohydrates, f.Sugars, f.Fats, f.SaturatedFats, f.Fiber, f.SaltGrams),
        f.IsVerified, f.CreatedByUserId == userId, f.EnergyWarning(), f.Category);
}

public sealed record FoodSummaryDto(
    Guid Id, string Name, string? Brand, double Calories, double Proteins,
    double Carbohydrates, double Fats, bool IsVerified, bool IsOwn);
