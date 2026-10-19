using FluentAssertions;
using NutriScan.Domain.Entities;
using Xunit;

namespace NutriScan.Domain.UnitTests.Entities;

public class UserAgeTests
{
    [Theory]
    [InlineData("1998-05-14", "2026-05-13", 27)]
    [InlineData("1998-05-14", "2026-05-14", 28)]
    [InlineData("2000-02-29", "2025-02-28", 24)]
    [InlineData("2000-02-29", "2025-03-01", 25)]
    public void AgeOn_CountsCompletedYears(string birth, string today, int expected)
    {
        // Arrange
        var user = new User { BirthDate = DateOnly.Parse(birth) };

        // Act
        var age = user.AgeOn(DateOnly.Parse(today));

        // Assert
        age.Should().Be(expected);
    }

    [Fact]
    public void AgeOn_WithoutBirthDate_ReturnsNull()
    {
        // Act
        var age = new User().AgeOn(new DateOnly(2026, 10, 16));

        // Assert
        age.Should().BeNull();
    }
}
