using FluentAssertions;
using NutriScan.Domain.Nutrition;
using Xunit;

namespace NutriScan.Domain.UnitTests.Nutrition;

public class EnergyCheckTests
{
    [Fact]
    public void Warning_Beyond15Percent_ReturnsMessage()
    {
        // Act: 13.1 g fat = 117.9 kcal against 100 declared (17.9%)
        var warning = EnergyCheck.Warning(100, 0, 0, 13.1);

        // Assert
        warning.Should().NotBeNull();
    }

    [Fact]
    public void Warning_Exactly15Percent_ReturnsNull()
    {
        // Act: 28.75 g carbs = 115 kcal against 100 declared
        var warning = EnergyCheck.Warning(100, 0, 28.75, 0);

        // Assert
        warning.Should().BeNull();
    }

    [Fact]
    public void Warning_ZeroEverything_ReturnsNull()
    {
        // Act: e.g. water
        var warning = EnergyCheck.Warning(0, 0, 0, 0);

        // Assert
        warning.Should().BeNull();
    }

    [Fact]
    public void Warning_ZeroCaloriesWithMacros_ReturnsMessage()
    {
        // Act
        var warning = EnergyCheck.Warning(0, 5, 0, 0);

        // Assert
        warning.Should().NotBeNull();
    }

    [Fact]
    public void Warning_HasExactRomanianMessage()
    {
        // Act: 4·10 + 4·30 + 9·5 = 205 kcal; |205 − 250| / 250 = 18%
        var warning = EnergyCheck.Warning(250, 10, 30, 5);

        // Assert
        warning.Should().Be("Caloriile declarate (250 kcal) diferă cu 18% de cele calculate din macronutrienți (205 kcal). Verifică valorile.");
    }

    [Fact]
    public void Tolerance_MatchesTargetCalculator()
    {
        // Assert
        EnergyCheck.Tolerance.Should().Be(NutritionTargetCalculator.EnergyTolerance);
    }
}
