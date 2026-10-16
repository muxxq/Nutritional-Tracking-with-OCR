using FluentAssertions;
using NutriScan.Application.Auth.Commands.Register;

namespace NutriScan.Application.UnitTests.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand Valid() => new("ana@exemplu.ro", "parola123", "Ana", "Pop");

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        // Act
        var result = _validator.Validate(Valid());

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(8)]
    [InlineData(128)]
    public void Validate_PasswordAtLengthBoundary_IsValid(int length)
    {
        // Arrange
        var command = Valid() with { Password = new string('p', length) };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    public static TheoryData<RegisterCommand, string> InvalidCommands => new()
    {
        { Valid() with { Email = "" }, nameof(RegisterCommand.Email) },
        { Valid() with { Email = "nu-e-email" }, nameof(RegisterCommand.Email) },
        { Valid() with { Email = new string('a', 246) + "@exemplu.ro" }, nameof(RegisterCommand.Email) },
        { Valid() with { Password = new string('p', 7) }, nameof(RegisterCommand.Password) },
        { Valid() with { Password = new string('p', 129) }, nameof(RegisterCommand.Password) },
        { Valid() with { FirstName = "   " }, nameof(RegisterCommand.FirstName) },
        { Valid() with { LastName = new string('n', 101) }, nameof(RegisterCommand.LastName) }
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public void Validate_InvalidField_ReportsErrorOnThatField(RegisterCommand command, string property)
    {
        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().OnlyContain(e => e.PropertyName == property);
    }
}
