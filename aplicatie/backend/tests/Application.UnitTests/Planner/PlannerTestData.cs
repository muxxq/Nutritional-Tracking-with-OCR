using NutriScan.Application.Planner.Commands.CreatePlannedMeal;
using NutriScan.Application.Planner.Commands.UpdatePlannedMeal;
using NutriScan.Application.UnitTests.Diary;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Planner;

public static class PlannerTestData
{
    public static readonly DateOnly Today = DiaryTestData.Today;

    public static CreatePlannedMealCommand Food(Guid userId, Guid foodId, double? grams = 100, double? servings = null,
        MealType meal = MealType.Lunch, DateOnly? date = null) =>
        new(userId, foodId, null, date ?? Today, meal, grams, servings);

    public static CreatePlannedMealCommand Recipe(Guid userId, Guid recipeId, double? servings = 2, double? grams = null,
        MealType meal = MealType.Lunch, DateOnly? date = null) =>
        new(userId, null, recipeId, date ?? Today, meal, grams, servings);

    public static UpdatePlannedMealCommand Update(Guid userId, Guid mealId, double? grams = null, double? servings = 1,
        MealType meal = MealType.Dinner, DateOnly? date = null) =>
        new(userId, mealId, date ?? Today.AddDays(1), meal, grams, servings);
}
