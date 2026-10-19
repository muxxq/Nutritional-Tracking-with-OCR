using FluentAssertions;
using Moq;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Planner;
using NutriScan.Application.Planner.Commands.CreatePlannedMeal;

namespace NutriScan.Application.UnitTests.Planner;

public class PlannedMealValidatorTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    private readonly CreatePlannedMealCommandValidator _validator;

    public PlannedMealValidatorTests()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now);
        _validator = new CreatePlannedMealCommandValidator(clock.Object);
    }

    private static CreatePlannedMealCommand Valid() => PlannerTestData.Food(Guid.NewGuid(), Guid.NewGuid()) with { Date = Today };

    [Fact]
    public void Validate_FoodAndRecipe_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { RecipeId = Guid.NewGuid() });

        // Assert
        result.Errors.Should().Contain(e => e.ErrorMessage == PlannerErrors.ExactlyOneItem);
    }

    [Fact]
    public void Validate_NeitherFoodNorRecipe_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { FoodId = null });

        // Assert
        result.Errors.Should().Contain(e => e.ErrorMessage == PlannerErrors.ExactlyOneItem);
    }

    [Fact]
    public void Validate_RecipeWithGrams_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { FoodId = null, RecipeId = Guid.NewGuid() });

        // Assert
        result.Errors.Should().Contain(e => e.ErrorMessage == PlannerErrors.RecipeServingsOnly);
    }

    [Fact]
    public void Validate_BothQuantities_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { Servings = 1 });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Grams5001_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { QuantityGrams = 5001 });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Servings51_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { QuantityGrams = null, Servings = 51 });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DateBefore2000_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { Date = new DateOnly(1999, 12, 31) });

        // Assert
        result.Errors.Should().Contain(e => e.ErrorMessage == PlannerErrors.DateRange);
    }

    [Fact]
    public void Validate_Date367DaysAhead_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { Date = Today.AddDays(367) });

        // Assert
        result.Errors.Should().Contain(e => e.ErrorMessage == PlannerErrors.DateRange);
    }

    [Fact]
    public void Validate_Date366DaysAhead_IsValid()
    {
        // Act
        var result = _validator.Validate(Valid() with { Date = Today.AddDays(366) });

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
