using NutriScan.Application.Diary.Commands.AddDiaryEntry;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Diary;

public static class DiaryTestData
{
    public static readonly DateOnly Today = new(2026, 10, 16);

    /// <summary>389 kcal, 16.9 P, 66.3 C, 6.9 F per 100 g; one serving is 40 g.</summary>
    public static FoodItem Oats(Guid? owner, double? servingSizeGrams = 40) => new()
    {
        Name = "Fulgi de ovăz", Brand = "Generic", CreatedByUserId = owner, ServingSizeGrams = servingSizeGrams,
        Calories = 389, Proteins = 16.9, Carbohydrates = 66.3, Fats = 6.9
    };

    public static FoodItem Chicken(Guid? owner = null) => new()
    {
        Name = "Piept de pui", CreatedByUserId = owner, Calories = 120, Proteins = 22.5, Carbohydrates = 0, Fats = 2.6
    };

    public static AddDiaryEntryCommand Add(Guid userId, Guid foodId, double? grams = 100, double? servings = null,
        MealType meal = MealType.Breakfast, DateOnly? date = null) =>
        new(userId, foodId, null, date ?? Today, meal, grams, servings);

    public static AddDiaryEntryCommand AddRecipe(Guid userId, Guid recipeId, double? servings = 1, double? grams = null,
        MealType meal = MealType.Breakfast, DateOnly? date = null) =>
        new(userId, null, recipeId, date ?? Today, meal, grams, servings);
}
