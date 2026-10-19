using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Entities;
using NutriScan.Infrastructure.Identity;
using System.Text;

namespace NutriScan.Infrastructure.UnitTests.Identity;

public class JwtTokenServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    private readonly JwtOptions _options = new()
    {
        Secret = "test-secret-that-is-at-least-32-bytes-long!",
        Issuer = "NutriScanApi",
        Audience = "NutriScanClient"
    };

    private JwtTokenService CreateService()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now);
        return new JwtTokenService(Options.Create(_options), clock.Object);
    }

    [Fact]
    public void CreateAccessToken_ExpiresAfterFifteenMinutes()
    {
        // Arrange
        var service = CreateService();

        // Act
        var token = service.CreateAccessToken(new User { Email = "ana@exemplu.ro" });

        // Assert
        token.ExpiresAtUtc.Should().Be(Now.AddMinutes(15));
    }

    [Fact]
    public async Task CreateAccessToken_ContainsSubjectAndEmailClaims()
    {
        // Arrange
        var service = CreateService();
        var user = new User { Email = "ana@exemplu.ro" };

        // Act
        var token = service.CreateAccessToken(user);
        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            ValidateLifetime = false
        });

        // Assert
        validation.IsValid.Should().BeTrue();
        validation.Claims[JwtRegisteredClaimNames.Sub].Should().Be(user.Id.ToString());
        validation.Claims[JwtRegisteredClaimNames.Email].Should().Be("ana@exemplu.ro");
        validation.Claims.Should().ContainKey(JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateRefreshToken_Returns32RandomBytesBase64Url()
    {
        // Arrange
        var service = CreateService();

        // Act
        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        // Assert
        first.Should().NotBe(second);
        Base64UrlEncoder.DecodeBytes(first).Should().HaveCount(32);
    }

    [Fact]
    public void HashRefreshToken_ReturnsLowercaseSha256Hex()
    {
        // Arrange
        var service = CreateService();

        // Act
        var hash = service.HashRefreshToken("abc");

        // Assert
        hash.Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
    }

    [Fact]
    public void RefreshTokenLifetime_IsSevenDays()
    {
        // Act
        var lifetime = CreateService().RefreshTokenLifetime;

        // Assert
        lifetime.Should().Be(TimeSpan.FromDays(7));
    }
}
