using NutriScan.Application.Foods.Commands.CreateFood;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.UnitTests.Foods;

public static class FoodTestData
{
    public static CreateFoodCommand Oats(Guid userId) => new(
        userId, "Fulgi de ovăz", "Generic", null, 40,
        Calories: 389, Proteins: 16.9, Carbohydrates: 66.3, Sugars: 1.0,
        Fats: 6.9, SaturatedFats: 1.2, Fiber: 10.6, SaltGrams: 0.01);

    public static FoodItem Food(string name, Guid? owner = null, bool verified = false) => new()
    {
        Name = name, CreatedByUserId = owner, IsVerified = verified,
        Calories = 120, Proteins = 22.5, Carbohydrates = 0, Fats = 2.6
    };
}
