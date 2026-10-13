using FluentAssertions;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;
using Xunit;

namespace NutriScan.Domain.UnitTests.Entities;

public class DiaryEntryTests
{
    [Fact]
    public void RecalculateNutrients_ShouldScaleValuesLinearly_BasedOn100gReference()
    {
        // Arrange
        var food = new FoodItem
        {
            Name = "Rolled Oats",
            Calories = 389.0,
            Proteins = 16.9,
            Carbohydrates = 66.3,
            Fats = 6.9
        };

        var entry = new DiaryEntry
        {
            FoodItem = food,
            QuantityGrams = 200.0,
            MealType = MealType.Breakfast,
            ConsumedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // Act
        entry.RecalculateNutrients();

        // Assert
        entry.CalculatedCalories.Should().Be(778.0);
        entry.CalculatedProteins.Should().Be(33.8);
        entry.CalculatedCarbs.Should().Be(132.6);
        entry.CalculatedFats.Should().Be(13.8);
    }
}
