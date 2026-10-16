using FluentAssertions;
using NutriScan.Application.Foods;
using NutriScan.Application.Foods.Commands.CreateFood;

namespace NutriScan.Application.UnitTests.Foods;

public class FoodFieldsValidatorTests
{
    private readonly FoodFieldsValidator _validator = new();

    private static CreateFoodCommand Valid() => FoodTestData.Oats(Guid.NewGuid());

    [Fact]
    public void Validate_Oats_IsValid()
    {
        // Act
        var result = _validator.Validate(Valid());

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OptionalFieldsNull_IsValid()
    {
        // Act
        var result = _validator.Validate(Valid() with
        {
            Brand = null, Barcode = null, ServingSizeGrams = null, Sugars = null, SaturatedFats = null, Fiber = null, SaltGrams = null
        });

        // Assert
        result.IsValid.Should().BeTrue();
    }

    public static TheoryData<CreateFoodCommand, bool> Cases => new()
    {
        { Valid() with { Calories = 900 }, true },
        { Valid() with { Calories = 901 }, false },
        { Valid() with { Calories = -0.1 }, false },
        { Valid() with { Proteins = 50, Carbohydrates = 50, Fats = 0, SaturatedFats = null }, true },
        { Valid() with { Proteins = 50, Carbohydrates = 49.95, Fats = 0.1, SaturatedFats = null }, false },
        { Valid() with { Proteins = -1 }, false },
        { Valid() with { Sugars = 66.4 }, false },
        { Valid() with { SaturatedFats = 7.0 }, false },
        { Valid() with { Fiber = 101 }, false },
        { Valid() with { SaltGrams = -0.1 }, false },
        { Valid() with { Barcode = "1234567" }, false },
        { Valid() with { Barcode = "12345678" }, true },
        { Valid() with { Barcode = "12345678901234" }, true },
        { Valid() with { Barcode = "123456789012345" }, false },
        { Valid() with { Barcode = "12345678a" }, false },
        { Valid() with { ServingSizeGrams = 0 }, false },
        { Valid() with { ServingSizeGrams = 1 }, true },
        { Valid() with { ServingSizeGrams = 2000 }, true },
        { Valid() with { ServingSizeGrams = 2001 }, false },
        { Valid() with { Name = "" }, false },
        { Valid() with { Name = new string('a', 251) }, false },
        { Valid() with { Brand = new string('b', 151) }, false }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate_Limits(CreateFoodCommand command, bool valid)
    {
        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().Be(valid);
    }

    [Fact]
    public void Validate_NameOfOnlyCombiningMarks_IsInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { Name = "́́" });

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
