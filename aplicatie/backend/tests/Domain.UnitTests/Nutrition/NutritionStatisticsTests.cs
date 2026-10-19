using FluentAssertions;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Domain.UnitTests.Nutrition;

public class NutritionStatisticsTests
{
    private static readonly Macros Target = new(1700, 124, 195, 47);

    private static Macros Kcal(double calories) => new(calories, 100, 200, 50);

    [Fact]
    public void Summarize_ThreeLoggedDays_AveragesOnlyThoseDays()
    {
        // Act
        var summary = NutritionStatistics.Summarize([Kcal(1500), Kcal(1700), Kcal(1900)], Target);

        // Assert
        summary.LoggedDays.Should().Be(3);
        summary.Average.Should().Be(new Macros(1700, 100, 200, 50));
    }

    [Fact]
    public void Summarize_AboveTarget_DeviationIsSignedAndInPercent()
    {
        // Act
        var summary = NutritionStatistics.Summarize([Kcal(1785)], Target);

        // Assert
        summary.Deviation.Should().Be(new Macros(85, -24, 5, 3));
        summary.CaloriesDeviationPercent.Should().BeApproximately(5, 1e-9);
    }

    [Fact]
    public void Summarize_CountsDaysByCalorieStatus()
    {
        // Act: 1615–1785 kcal is within ±5% of 1700
        var summary = NutritionStatistics.Summarize([Kcal(1000), Kcal(1615), Kcal(1785), Kcal(1786), Kcal(2500)], Target);

        // Assert
        summary.StatusCounts.Should().Be(new DayStatusCounts(Under: 1, Within: 2, Over: 2));
    }

    [Fact]
    public void Summarize_NoLoggedDays_HasNoAverageOrDeviation()
    {
        // Act
        var summary = NutritionStatistics.Summarize([], Target);

        // Assert
        summary.LoggedDays.Should().Be(0);
        summary.Average.Should().BeNull();
        summary.Deviation.Should().BeNull();
        summary.CaloriesDeviationPercent.Should().BeNull();
        summary.StatusCounts.Should().Be(new DayStatusCounts(0, 0, 0));
    }

    [Fact]
    public void Summarize_ZeroCalorieTarget_HasNoPercent()
    {
        // Act
        var summary = NutritionStatistics.Summarize([Kcal(100)], Target with { Calories = 0 });

        // Assert
        summary.Deviation!.Calories.Should().Be(100);
        summary.CaloriesDeviationPercent.Should().BeNull();
    }
}
