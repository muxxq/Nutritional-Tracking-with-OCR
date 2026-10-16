using FluentAssertions;
using NutriScan.Domain.Nutrition;
using Xunit;

namespace NutriScan.Domain.UnitTests.Nutrition;

public class DiaryQuantityTests
{
    [Fact]
    public void Resolve_Grams_ReturnsGramsWithoutServings()
    {
        // Act
        var result = DiaryQuantity.Resolve(150, null, 40);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.QuantityGrams.Should().Be(150);
        result.Servings.Should().BeNull();
    }

    [Fact]
    public void Resolve_Servings_ConvertsWithServingSize()
    {
        // Act
        var result = DiaryQuantity.Resolve(null, 1.5, 40);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.QuantityGrams.Should().Be(60);
        result.Servings.Should().Be(1.5);
    }

    [Fact]
    public void Resolve_BothQuantities_ReturnsError() =>
        DiaryQuantity.Resolve(100, 1, 40).Error.Should().Be(DiaryQuantity.ExactlyOneQuantity);

    [Fact]
    public void Resolve_NoQuantity_ReturnsError() =>
        DiaryQuantity.Resolve(null, null, 40).Error.Should().Be(DiaryQuantity.ExactlyOneQuantity);

    [Fact]
    public void Resolve_ServingsWithoutServingSize_ReturnsError() =>
        DiaryQuantity.Resolve(null, 2, null).Error.Should().Be(DiaryQuantity.NoServingSize);

    [Fact]
    public void Resolve_ServingsAboveLimitAfterConversion_ReturnsError()
    {
        // Act
        var tooMuch = DiaryQuantity.Resolve(null, 50, 200);
        var atLimit = DiaryQuantity.Resolve(null, 25, 200);

        // Assert
        tooMuch.Error.Should().Be(DiaryQuantity.TooMuch);
        atLimit.IsSuccess.Should().BeTrue();
        atLimit.QuantityGrams.Should().Be(5000);
    }
}
