using FluentAssertions;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;
using Xunit;

namespace NutriScan.Domain.UnitTests.Entities;

public class DiaryEntryTests
{
    private static FoodItem Oats() => new()
    {
        Name = "Fulgi de ovăz",
        Calories = 389.0,
        Proteins = 16.9,
        Carbohydrates = 66.3,
        Fats = 6.9
    };

    [Fact]
    public void RecalculateNutrients_ScalesPer100g()
    {
        // Arrange
        var entry = new DiaryEntry { QuantityGrams = 60, MealType = MealType.Breakfast };

        // Act
        entry.RecalculateNutrients(Oats());

        // Assert: values are kept unrounded, rounding happens only for display
        entry.CalculatedCalories.Should().BeApproximately(233.4, 1e-9);
        entry.CalculatedProteins.Should().BeApproximately(10.14, 1e-9);
        entry.CalculatedCarbs.Should().BeApproximately(39.78, 1e-9);
        entry.CalculatedFats.Should().BeApproximately(4.14, 1e-9);
    }

    [Fact]
    public void RecalculateNutrients_UsesGivenFood_NotNavigation()
    {
        // Arrange
        var entry = new DiaryEntry { QuantityGrams = 200 };

        // Act
        entry.RecalculateNutrients(Oats());

        // Assert
        entry.FoodItem.Should().BeNull();
        entry.CalculatedCalories.Should().BeApproximately(778.0, 1e-9);
    }
}
