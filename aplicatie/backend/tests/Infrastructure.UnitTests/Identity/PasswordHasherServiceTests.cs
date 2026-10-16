using FluentAssertions;
using NutriScan.Application.Common.Models;
using NutriScan.Infrastructure.Identity;

namespace NutriScan.Infrastructure.UnitTests.Identity;

public class PasswordHasherServiceTests
{
    private readonly PasswordHasherService _hasher = new();

    [Fact]
    public void Verify_CorrectPassword_ReturnsSuccess()
    {
        // Arrange
        var hash = _hasher.Hash("parola-buna");

        // Act
        var result = _hasher.Verify(hash, "parola-buna");

        // Assert
        result.Should().Be(PasswordCheckResult.Success);
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFailed()
    {
        // Arrange
        var hash = _hasher.Hash("parola-buna");

        // Act
        var result = _hasher.Verify(hash, "parola-gresita");

        // Assert
        result.Should().Be(PasswordCheckResult.Failed);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // Act
        var first = _hasher.Hash("parola-buna");
        var second = _hasher.Hash("parola-buna");

        // Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void Verify_AgainstDummyHash_ReturnsFailed()
    {
        // Act
        var result = _hasher.Verify(_hasher.DummyHash, "orice");

        // Assert
        result.Should().Be(PasswordCheckResult.Failed);
    }
}
