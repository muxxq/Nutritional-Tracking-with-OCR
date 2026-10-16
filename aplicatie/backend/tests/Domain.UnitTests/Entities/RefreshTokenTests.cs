using FluentAssertions;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Exceptions;
using Xunit;

namespace NutriScan.Domain.UnitTests.Entities;

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private static RefreshToken CreateToken() =>
        RefreshToken.Create(Guid.NewGuid(), "hash", Guid.NewGuid(), Now, Lifetime);

    [Fact]
    public void Create_SetsExpiryFromLifetime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();

        // Act
        var token = RefreshToken.Create(userId, "hash", familyId, Now, Lifetime);

        // Assert
        token.UserId.Should().Be(userId);
        token.FamilyId.Should().Be(familyId);
        token.TokenHash.Should().Be("hash");
        token.CreatedAtUtc.Should().Be(Now);
        token.ExpiresAtUtc.Should().Be(Now + Lifetime);
        token.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void IsActive_AfterExpiry_ReturnsFalse()
    {
        // Arrange
        var token = CreateToken();

        // Act
        var expiry = Now + Lifetime;

        // Assert
        token.IsExpired(expiry).Should().BeTrue();
        token.IsActive(expiry).Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsRevokedAtAndReplacement()
    {
        // Arrange
        var token = CreateToken();
        var replacementId = Guid.NewGuid();

        // Act
        token.Revoke(Now, replacementId);

        // Assert
        token.RevokedAtUtc.Should().Be(Now);
        token.ReplacedByTokenId.Should().Be(replacementId);
        token.IsRevoked.Should().BeTrue();
        token.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsDomainException()
    {
        // Arrange
        var token = CreateToken();
        token.Revoke(Now);

        // Act
        var act = () => token.Revoke(Now);

        // Assert
        act.Should().Throw<DomainException>();
    }
}
