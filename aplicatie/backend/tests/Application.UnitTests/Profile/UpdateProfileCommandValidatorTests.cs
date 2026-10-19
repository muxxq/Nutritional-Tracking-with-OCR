using FluentAssertions;
using Moq;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Profile.Commands.UpdateProfile;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Profile;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator;

    public UpdateProfileCommandValidatorTests()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc));
        _validator = new UpdateProfileCommandValidator(clock.Object);
    }

    private static UpdateProfileCommand Valid() => ProfileTestData.R1(Guid.NewGuid());

    [Fact]
    public void Validate_R1_IsValid()
    {
        // Act
        var result = _validator.Validate(Valid());

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("2008-10-16", true)]   // 18
    [InlineData("2008-10-17", false)]  // 17
    [InlineData("1926-10-17", true)]   // 99
    [InlineData("1926-10-16", true)]   // 100
    [InlineData("1925-10-16", false)]  // 101
    [InlineData("2030-01-01", false)]  // future
    public void Validate_BirthDate_AllowsAges18To100(string birthDate, bool valid)
    {
        // Act
        var result = _validator.Validate(Valid() with { BirthDate = DateOnly.Parse(birthDate) });

        // Assert
        result.IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData(99, false)]
    [InlineData(100, true)]
    [InlineData(250, true)]
    [InlineData(251, false)]
    public void Validate_Height_Allows100To250(double heightCm, bool valid)
    {
        // Act
        var result = _validator.Validate(Valid() with { HeightCm = heightCm });

        // Assert
        result.IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData(29, false)]
    [InlineData(30, true)]
    [InlineData(300, true)]
    [InlineData(301, false)]
    public void Validate_Weight_Allows30To300(double weightKg, bool valid)
    {
        // Act
        var result = _validator.Validate(Valid() with { WeightKg = weightKg });

        // Assert
        result.IsValid.Should().Be(valid);
    }

    [Fact]
    public void Validate_UnknownEnumValues_AreInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with
        {
            Gender = (Gender)9, ActivityLevel = (ActivityLevel)9, Goal = (NutritionGoal)9
        });

        // Assert
        result.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(
            nameof(UpdateProfileCommand.Gender), nameof(UpdateProfileCommand.ActivityLevel), nameof(UpdateProfileCommand.Goal));
    }

    [Fact]
    public void Validate_BlankNames_AreInvalid()
    {
        // Act
        var result = _validator.Validate(Valid() with { FirstName = " ", LastName = "" });

        // Assert
        result.Errors.Select(e => e.PropertyName).Should().Contain(new[] { "FirstName", "LastName" });
    }
}
