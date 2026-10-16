using FluentAssertions;
using NutriScan.Application.Profile.Commands.UpdateTargets;

namespace NutriScan.Application.UnitTests.Profile;

public class UpdateTargetsCommandValidatorTests
{
    private readonly UpdateTargetsCommandValidator _validator = new();

    private static UpdateTargetsCommand None() => new(Guid.NewGuid(), null, null, null, null);

    [Fact]
    public void Validate_AllNull_IsValid()
    {
        // Act
        var result = _validator.Validate(None());

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(999, false)]
    [InlineData(1000, true)]
    [InlineData(6000, true)]
    [InlineData(6001, false)]
    public void Validate_Calories_Allows1000To6000(int calories, bool valid)
    {
        // Act
        var result = _validator.Validate(None() with { Calories = calories });

        // Assert
        result.IsValid.Should().Be(valid);
    }

    public static TheoryData<UpdateTargetsCommand, string> OutOfRange => new()
    {
        { None() with { ProteinsGrams = -1 }, nameof(UpdateTargetsCommand.ProteinsGrams) },
        { None() with { ProteinsGrams = 501 }, nameof(UpdateTargetsCommand.ProteinsGrams) },
        { None() with { CarbsGrams = 1001 }, nameof(UpdateTargetsCommand.CarbsGrams) },
        { None() with { FatsGrams = 301 }, nameof(UpdateTargetsCommand.FatsGrams) }
    };

    [Theory]
    [MemberData(nameof(OutOfRange))]
    public void Validate_MacroOutOfRange_IsInvalid(UpdateTargetsCommand command, string property)
    {
        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == property);
    }
}
