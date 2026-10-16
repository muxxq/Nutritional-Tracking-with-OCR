using NutriScan.Application.Common.Models;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.Diary.Dtos;

public sealed record DiaryFoodDto(Guid Id, string Name, string? Brand, bool IsArchived)
{
    public static DiaryFoodDto From(FoodItem f) => new(f.Id, f.Name, f.Brand, f.ArchivedAtUtc is not null);
}

public sealed record DiaryEntryDto(
    Guid Id, DateOnly Date, MealType MealType, double QuantityGrams, double? Servings,
    DiaryFoodDto Food, NutrientsDto Nutrients)
{
    public static DiaryEntryDto From(DiaryEntry entry, FoodItem food) => new(
        entry.Id, entry.ConsumedDate, entry.MealType, entry.QuantityGrams!.Value, entry.Servings,
        DiaryFoodDto.From(food), NutrientsDto.Of(entry).Rounded());
}

public sealed record DiaryMealDto(MealType MealType, IReadOnlyList<DiaryEntryDto> Entries, NutrientsDto Totals);

public sealed record DiaryDayDto(
    DateOnly Date, IReadOnlyList<DiaryMealDto> Meals, NutrientsDto Totals,
    NutrientsDto Targets, NutrientsDto Remaining, bool IsProfileComplete);

public sealed record RecentFoodDto(DiaryFoodDto Food, double LastQuantityGrams, double? LastServings, DateTime LastUsedAtUtc);
