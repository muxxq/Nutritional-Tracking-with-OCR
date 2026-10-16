using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.Diary.Dtos;

public sealed record NutrientsDto(double Calories, double Proteins, double Carbs, double Fats)
{
    public static readonly NutrientsDto Zero = new(0, 0, 0, 0);

    /// <summary>Unrounded snapshot of an entry; sums are built from these and rounded once, for display.</summary>
    public static NutrientsDto Of(DiaryEntry e) => new(e.CalculatedCalories, e.CalculatedProteins, e.CalculatedCarbs, e.CalculatedFats);

    public NutrientsDto Rounded() => new(Round(Calories), Round(Proteins), Round(Carbs), Round(Fats));

    public static NutrientsDto operator +(NutrientsDto a, NutrientsDto b) =>
        new(a.Calories + b.Calories, a.Proteins + b.Proteins, a.Carbs + b.Carbs, a.Fats + b.Fats);

    public static NutrientsDto operator -(NutrientsDto a, NutrientsDto b) =>
        new(a.Calories - b.Calories, a.Proteins - b.Proteins, a.Carbs - b.Carbs, a.Fats - b.Fats);

    // "+ 0.0" turns -0 (e.g. 0.03 kcal over target) into 0, which JSON would otherwise print as "-0".
    private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero) + 0.0;
}

public sealed record DiaryFoodDto(Guid Id, string Name, string? Brand, bool IsArchived)
{
    public static DiaryFoodDto From(FoodItem f) => new(f.Id, f.Name, f.Brand, f.ArchivedAtUtc is not null);
}

public sealed record DiaryEntryDto(
    Guid Id, DateOnly Date, MealType MealType, double QuantityGrams, double? Servings,
    DiaryFoodDto Food, NutrientsDto Nutrients)
{
    public static DiaryEntryDto From(DiaryEntry entry, FoodItem food) => new(
        entry.Id, entry.ConsumedDate, entry.MealType, entry.QuantityGrams, entry.Servings,
        DiaryFoodDto.From(food), NutrientsDto.Of(entry).Rounded());
}

public sealed record DiaryMealDto(MealType MealType, IReadOnlyList<DiaryEntryDto> Entries, NutrientsDto Totals);

public sealed record DiaryDayDto(
    DateOnly Date, IReadOnlyList<DiaryMealDto> Meals, NutrientsDto Totals,
    NutrientsDto Targets, NutrientsDto Remaining, bool IsProfileComplete);

public sealed record RecentFoodDto(DiaryFoodDto Food, double LastQuantityGrams, double? LastServings, DateTime LastUsedAtUtc);
