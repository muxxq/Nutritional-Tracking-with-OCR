using FluentAssertions;
using NutriScan.Infrastructure.Identity;

namespace NutriScan.Infrastructure.UnitTests.Identity;

public class JwtOptionsValidatorTests
{
    private static JwtOptions Options(string secret) => new()
    {
        Secret = secret,
        Issuer = "NutriScanApi",
        Audience = "NutriScanClient"
    };

    [Fact]
    public void Validate_SecretOf31Bytes_Fails()
    {
        // Arrange
        var validator = new JwtOptionsValidator();

        // Act
        var result = validator.Validate(null, Options(new string('a', 31)));

        // Assert
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptySecret_Fails()
    {
        // Arrange
        var validator = new JwtOptionsValidator();

        // Act
        var result = validator.Validate(null, Options(string.Empty));

        // Assert
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingIssuer_Fails()
    {
        // Arrange
        var validator = new JwtOptionsValidator();
        var options = Options(new string('a', 32));
        options.Issuer = string.Empty;

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_SecretOf32BytesWithIssuerAndAudience_Succeeds()
    {
        // Arrange
        var validator = new JwtOptionsValidator();

        // Act
        var result = validator.Validate(null, Options(new string('a', 32)));

        // Assert
        result.Succeeded.Should().BeTrue();
    }
}
