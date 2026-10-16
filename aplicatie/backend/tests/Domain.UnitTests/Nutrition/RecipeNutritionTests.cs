using FluentAssertions;
using NutriScan.Domain.Nutrition;
using Xunit;
using static NutriScan.Domain.UnitTests.Nutrition.RecipeTestData;

namespace NutriScan.Domain.UnitTests.Nutrition;

public class RecipeNutritionTests
{
    [Fact]
    public void Of_SrsExample_PerServingIs194Point5()
    {
        // Act
        var result = RecipeNutrition.Of(SrsRecipe());

        // Assert
        result.Total.Calories.Should().Be(778);
        result.PerServing.Calories.Should().Be(194.5);
        result.PerServing.Proteins.Should().BeApproximately(8.45, 1e-9);
    }

    [Fact]
    public void Of_TwoIngredients_SumsContributions()
    {
        // Arrange
        var recipe = Recipe(2, (Oats(), 200), (Chicken(), 150));

        // Act
        var result = RecipeNutrition.Of(recipe);

        // Assert
        result.Ingredients.Select(i => i.Calories).Should().Equal(778, 180);
        result.Total.Calories.Should().Be(958);
        result.PerServing.Calories.Should().Be(479);
        result.Total.Proteins.Should().BeApproximately(33.8 + 33.75, 1e-9);
    }
}
