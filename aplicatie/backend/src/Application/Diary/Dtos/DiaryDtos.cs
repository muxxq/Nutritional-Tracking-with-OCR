using NutriScan.Application.Common.Models;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.Diary.Dtos;

public sealed record DiaryFoodDto(Guid Id, string Name, string? Brand, bool IsArchived)
{
    public static DiaryFoodDto From(FoodItem f) => new(f.Id, f.Name, f.Brand, f.ArchivedAtUtc is not null);
}

public enum DiaryEntryKind { Food, Recipe }

public sealed record DiaryRecipeDto(Guid Id, string Name, bool IsArchived)
{
    public static DiaryRecipeDto From(Recipe r) => new(r.Id, r.Name, r.ArchivedAtUtc is not null);
}

/// <summary>Exactly one of <see cref="Food"/> / <see cref="Recipe"/> is set, as <see cref="Kind"/> says.</summary>
public sealed record DiaryEntryDto(
    Guid Id, DiaryEntryKind Kind, DateOnly Date, MealType MealType, double? QuantityGrams, double? Servings,
    DiaryFoodDto? Food, DiaryRecipeDto? Recipe, NutrientsDto Nutrients)
{
    /// <summary>The entry must be loaded with its food or its recipe.</summary>
    public static DiaryEntryDto From(DiaryEntry entry) => new(
        entry.Id, entry.IsRecipe ? DiaryEntryKind.Recipe : DiaryEntryKind.Food,
        entry.ConsumedDate, entry.MealType, entry.QuantityGrams, entry.Servings,
        entry.IsRecipe ? null : DiaryFoodDto.From(entry.FoodItem!),
        entry.IsRecipe ? DiaryRecipeDto.From(entry.Recipe!) : null,
        NutrientsDto.Of(entry).Rounded());
}

public sealed record DiaryMealDto(MealType MealType, IReadOnlyList<DiaryEntryDto> Entries, NutrientsDto Totals);

public sealed record DiaryDayDto(
    DateOnly Date, IReadOnlyList<DiaryMealDto> Meals, NutrientsDto Totals,
    NutrientsDto Targets, NutrientsDto Remaining, bool IsProfileComplete);

public sealed record RecentFoodDto(DiaryFoodDto Food, double LastQuantityGrams, double? LastServings, DateTime LastUsedAtUtc);
