using NutriScan.Application.Common.Models;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Planner.Dtos;

/// <summary>Exactly one of <see cref="Food"/> / <see cref="Recipe"/> is set, as <see cref="Kind"/> says; values are current.</summary>
public sealed record PlannedMealDto(
    Guid Id, DiaryEntryKind Kind, DateOnly Date, MealType MealType, double? QuantityGrams, double? Servings,
    DiaryFoodDto? Food, DiaryRecipeDto? Recipe, NutrientsDto Nutrients, Guid? DiaryEntryId)
{
    /// <summary>The meal must be loaded with its food, or its recipe and the ingredients' foods.</summary>
    public static PlannedMealDto From(PlannedMeal meal) => new(
        meal.Id, meal.IsRecipe ? DiaryEntryKind.Recipe : DiaryEntryKind.Food,
        meal.PlanDate, meal.MealType, meal.QuantityGrams, meal.Servings,
        meal.IsRecipe ? null : DiaryFoodDto.From(meal.FoodItem!),
        meal.IsRecipe ? DiaryRecipeDto.From(meal.Recipe!) : null,
        NutrientsDto.From(meal.Nutrients()).Rounded(),
        meal.DiaryEntryId);
}

public sealed record PlanMealDto(MealType MealType, IReadOnlyList<PlannedMealDto> Items, NutrientsDto Totals);

/// <summary>A planned day against the calorie target (FR-10); the status comes from the unrounded total.</summary>
public sealed record PlanDayDto(
    DateOnly Date, IReadOnlyList<PlanMealDto> Meals, NutrientsDto Totals, NutrientsDto Targets, PlanDayStatus Status);

public sealed record PlanDto(DateOnly From, DateOnly To, IReadOnlyList<PlanDayDto> Days, bool IsProfileComplete);
