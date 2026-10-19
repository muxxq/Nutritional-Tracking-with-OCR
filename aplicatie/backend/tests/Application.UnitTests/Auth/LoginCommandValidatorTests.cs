using FluentAssertions;
using NutriScan.Application.Auth.Commands.Login;

namespace NutriScan.Application.UnitTests.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Theory]
    [InlineData("", "parola123", nameof(LoginCommand.Email))]
    [InlineData("ana@exemplu.ro", "", nameof(LoginCommand.Password))]
    public void Validate_EmptyField_ReportsError(string email, string password, string property)
    {
        // Act
        var result = _validator.Validate(new LoginCommand(email, password));

        // Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == property);
    }

    [Fact]
    public void Validate_PasswordOver128Chars_ReportsError()
    {
        // Act
        var result = _validator.Validate(new LoginCommand("ana@exemplu.ro", new string('p', 129)));

        // Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(LoginCommand.Password));
    }

    [Fact]
    public void Validate_ShortPassword_IsAcceptedToAvoidRevealingPolicy()
    {
        // Act
        var result = _validator.Validate(new LoginCommand("ana@exemplu.ro", "abc"));

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
