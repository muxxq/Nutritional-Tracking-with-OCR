using FluentAssertions;
using NutriScan.Domain.Entities;
using Xunit;
using static NutriScan.Domain.UnitTests.Nutrition.RecipeTestData;

namespace NutriScan.Domain.UnitTests.Entities;

public class PlannedMealTests
{
    [Fact]
    public void Nutrients_Food_ScalesPer100g()
    {
        // Arrange
        var oats = Oats();
        var meal = new PlannedMeal { FoodItem = oats, FoodItemId = oats.Id, QuantityGrams = 150 };

        // Act
        var nutrients = meal.Nutrients();

        // Assert
        nutrients.Calories.Should().Be(583.5);
        meal.IsRecipe.Should().BeFalse();
    }

    [Fact]
    public void Nutrients_Recipe_PerServingTimesServings()
    {
        // Arrange: 194.5 kcal per serving
        var recipe = SrsRecipe();
        var meal = new PlannedMeal { Recipe = recipe, RecipeId = recipe.Id, Servings = 2 };

        // Act
        var nutrients = meal.Nutrients();

        // Assert
        nutrients.Calories.Should().Be(389);
        meal.IsRecipe.Should().BeTrue();
    }
}
