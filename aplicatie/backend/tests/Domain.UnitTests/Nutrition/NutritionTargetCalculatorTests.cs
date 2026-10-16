using FluentAssertions;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;
using NutriScan.Domain.Nutrition;
using Xunit;

namespace NutriScan.Domain.UnitTests.Nutrition;

/// <summary>Reference cases R1–R5 come from spec §3.6 and were computed by hand.</summary>
public class NutritionTargetCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 16);

    private static DateOnly BornYearsAgo(int years) => Today.AddYears(-years).AddDays(-1);

    // R1: woman, 62 kg, 168 cm, 28 years, moderately active, losing weight
    private static User R1() => new()
    {
        Gender = Gender.Female, WeightKg = 62, HeightCm = 168, BirthDate = BornYearsAgo(28),
        ActivityLevel = ActivityLevel.ModeratelyActive, Goal = NutritionGoal.LoseWeight
    };

    private static NutritionTargets Valid(User user)
    {
        var result = NutritionTargetCalculator.Calculate(user, Today);
        result.IsValid.Should().BeTrue(result.Error);
        return result.Targets!;
    }

    private static void ShouldBe(NutritionTargets t, int? bmr, int? tdee, double kcal, double p, double c, double f)
    {
        t.Bmr.Should().Be(bmr);
        t.Tdee.Should().Be(tdee);
        t.Calories.Value.Should().Be(kcal);
        t.ProteinsGrams.Value.Should().Be(p);
        t.CarbsGrams.Value.Should().Be(c);
        t.FatsGrams.Value.Should().Be(f);
    }

    [Fact]
    public void Calculate_R1_WomanLosingWeight()
    {
        // Act
        var t = Valid(R1());

        // Assert
        ShouldBe(t, 1369, 2122, 1700, 124, 195, 47);
        t.Calories.Source.Should().Be(TargetSource.Calculated);
        t.CarbsGrams.Source.Should().Be(TargetSource.Calculated);
    }

    [Fact]
    public void Calculate_R2_ManualProteins_CarbsAbsorbDifference()
    {
        // Arrange
        var user = R1();
        user.ProteinsGramsOverride = 140;

        // Act
        var t = Valid(user);

        // Assert
        ShouldBe(t, 1369, 2122, 1700, 140, 179, 47);
        t.ProteinsGrams.Source.Should().Be(TargetSource.Manual);
    }

    [Fact]
    public void Calculate_R3_ManualCalories_MacrosFollow()
    {
        // Arrange
        var user = R1();
        user.CaloriesOverride = 1800;

        // Act
        var t = Valid(user);

        // Assert
        ShouldBe(t, 1369, 2122, 1800, 124, 214, 50);
        t.Calories.Source.Should().Be(TargetSource.Manual);
    }

    [Fact]
    public void Calculate_R4_ManSedentaryMaintaining()
    {
        // Arrange
        var user = new User
        {
            Gender = Gender.Male, WeightKg = 80, HeightCm = 180, BirthDate = BornYearsAgo(30),
            ActivityLevel = ActivityLevel.Sedentary, Goal = NutritionGoal.MaintainWeight
        };

        // Act
        var t = Valid(user);

        // Assert
        ShouldBe(t, 1780, 2136, 2140, 128, 273, 59);
    }

    [Fact]
    public void Calculate_R5_SafetyFloorForWomen()
    {
        // Arrange
        var user = new User
        {
            Gender = Gender.Female, WeightKg = 45, HeightCm = 150, BirthDate = BornYearsAgo(60),
            ActivityLevel = ActivityLevel.Sedentary, Goal = NutritionGoal.LoseWeight
        };

        // Act
        var t = Valid(user);

        // Assert
        ShouldBe(t, 927, 1112, 1200, 90, 135, 33);
    }

    [Fact]
    public void Calculate_ManualCarbs_FatsAbsorbDifference()
    {
        // Arrange
        var user = R1();
        user.CarbsGramsOverride = 150;

        // Act
        var t = Valid(user);

        // Assert
        t.CarbsGrams.Should().Be(new TargetValue(150, TargetSource.Manual));
        t.FatsGrams.Should().Be(new TargetValue(67, TargetSource.Calculated));
    }

    [Fact]
    public void Calculate_ManualCarbsAndFatsWithoutCalories_DerivesCalories()
    {
        // Arrange
        var user = R1();
        user.CarbsGramsOverride = 200;
        user.FatsGramsOverride = 60;

        // Act
        var t = Valid(user);

        // Assert
        t.Calories.Should().Be(new TargetValue(1840, TargetSource.Calculated));
    }

    [Fact]
    public void Calculate_AllManualWithinTolerance_IsValid()
    {
        // Arrange
        var user = R1();
        (user.CaloriesOverride, user.ProteinsGramsOverride, user.CarbsGramsOverride, user.FatsGramsOverride) = (2000, 150, 200, 65);

        // Act
        var t = Valid(user);

        // Assert
        ShouldBe(t, 1369, 2122, 2000, 150, 200, 65);
    }

    [Fact]
    public void Calculate_AllManualBeyondTolerance_ReturnsError()
    {
        // Arrange
        var user = R1();
        (user.CaloriesOverride, user.ProteinsGramsOverride, user.CarbsGramsOverride, user.FatsGramsOverride) = (2000, 100, 100, 30);

        // Act
        var result = NutritionTargetCalculator.Calculate(user, Today);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("15%");
    }

    [Fact]
    public void Calculate_ManualProteinsExceedCalories_ReturnsError()
    {
        // Arrange
        var user = R1();
        user.CaloriesOverride = 1000;
        user.ProteinsGramsOverride = 300;

        // Act
        var result = NutritionTargetCalculator.Calculate(user, Today);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("negativ");
    }

    [Fact]
    public void Calculate_IncompleteProfile_ReturnsDefaults()
    {
        // Act
        var t = Valid(new User());

        // Assert
        ShouldBe(t, null, null, 2000, 150, 200, 65);
        t.Calories.Source.Should().Be(TargetSource.Default);
        t.FatsGrams.Source.Should().Be(TargetSource.Default);
    }

    [Fact]
    public void Calculate_IncompleteProfileWithManualCalories_MixesManualAndDefault()
    {
        // Act
        var t = Valid(new User { CaloriesOverride = 1800 });

        // Assert
        t.Calories.Should().Be(new TargetValue(1800, TargetSource.Manual));
        t.ProteinsGrams.Should().Be(new TargetValue(150, TargetSource.Default));
    }

    [Fact]
    public void Calculate_IncompleteProfileAllManualInconsistent_ReturnsError()
    {
        // Arrange
        var user = new User
        {
            CaloriesOverride = 2000, ProteinsGramsOverride = 100, CarbsGramsOverride = 100, FatsGramsOverride = 30
        };

        // Act
        var result = NutritionTargetCalculator.Calculate(user, Today);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
