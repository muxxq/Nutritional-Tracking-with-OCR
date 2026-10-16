using FluentAssertions;
using NutriScan.Domain.Entities;
using Xunit;

namespace NutriScan.Domain.UnitTests.Entities;

public class UserLockoutTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    private static void FailTimes(User user, int times)
    {
        for (var i = 0; i < times; i++)
        {
            user.RegisterFailedLogin(Now);
        }
    }

    [Fact]
    public void RegisterFailedLogin_FourTimes_DoesNotLock()
    {
        // Arrange
        var user = new User();

        // Act
        FailTimes(user, 4);

        // Assert
        user.IsLockedOut(Now).Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(4);
    }

    [Fact]
    public void RegisterFailedLogin_FifthTime_LocksForFiveMinutes()
    {
        // Arrange
        var user = new User();

        // Act
        FailTimes(user, 5);

        // Assert
        user.LockoutEndUtc.Should().Be(Now.AddMinutes(5));
        user.IsLockedOut(Now.AddMinutes(5).AddSeconds(-1)).Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public void IsLockedOut_AfterLockoutEnds_ReturnsFalse()
    {
        // Arrange
        var user = new User();
        FailTimes(user, 5);

        // Act
        var lockedOut = user.IsLockedOut(Now.AddMinutes(5));

        // Assert
        lockedOut.Should().BeFalse();
    }

    [Fact]
    public void RegisterSuccessfulLogin_ResetsCounterAndLockout()
    {
        // Arrange
        var user = new User();
        FailTimes(user, 5);
        user.RegisterFailedLogin(Now);

        // Act
        user.RegisterSuccessfulLogin();

        // Assert
        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEndUtc.Should().BeNull();
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(170, 0, false)]
    [InlineData(0, 70, false)]
    [InlineData(170, 70, true)]
    public void IsProfileComplete_DependsOnHeightAndWeight(double heightCm, double weightKg, bool expected)
    {
        // Arrange
        var user = new User { HeightCm = heightCm, WeightKg = weightKg };

        // Act
        var complete = user.IsProfileComplete;

        // Assert
        complete.Should().Be(expected);
    }
}
