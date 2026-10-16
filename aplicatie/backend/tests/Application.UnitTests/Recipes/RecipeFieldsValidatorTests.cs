using FluentAssertions;
using NutriScan.Application.Recipes;
using NutriScan.Application.Recipes.Commands.CreateRecipe;

namespace NutriScan.Application.UnitTests.Recipes;

public class RecipeFieldsValidatorTests
{
    private readonly RecipeFieldsValidator _validator = new();

    private static CreateRecipeCommand Valid() => RecipeTestData.Create(Guid.NewGuid(), (Guid.NewGuid(), 200));

    private static List<RecipeIngredientInput> Ingredients(int count, double grams = 100) =>
        Enumerable.Range(0, count).Select(_ => new RecipeIngredientInput(Guid.NewGuid(), grams)).ToList();

    [Fact]
    public void Validate_ValidRecipe_IsValid()
    {
        // Act
        var result = _validator.Validate(Valid());

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_BlankName_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { Name = "   " });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Name201Chars_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { Name = new string('a', 201) });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Description1001Chars_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { Description = new string('a', 1001) });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Instructions10001Chars_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { Instructions = new string('a', 10_001) });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_ServingsCount0Or51_IsInvalid(int servings)
    {
        // Act
        var result = _validator.Validate(Valid() with { ServingsCount = servings });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1441)]
    public void Validate_PrepTimeNegativeOr1441_IsInvalid(int minutes)
    {
        // Act
        var result = _validator.Validate(Valid() with { PrepTimeMinutes = minutes });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NoIngredients_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { Ingredients = [] });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_51Ingredients_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { Ingredients = Ingredients(51) });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DuplicateFood_IsInvalid()
    {
        // Arrange
        var foodId = Guid.NewGuid();

        // Act
        var result = _validator.Validate(Valid() with
        {
            Ingredients = [new RecipeIngredientInput(foodId, 100), new RecipeIngredientInput(foodId, 50)]
        });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == RecipeErrors.DuplicateIngredient);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5001)]
    public void Validate_IngredientGrams0Or5001_IsInvalid(double grams)
    {
        // Act
        var result = _validator.Validate(Valid() with { Ingredients = Ingredients(1, grams) });

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_IngredientGrams5000_IsValid()
    {
        // Act
        var result = _validator.Validate(Valid() with { Ingredients = Ingredients(1, 5000) });

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
