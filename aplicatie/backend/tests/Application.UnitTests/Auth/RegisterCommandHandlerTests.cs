using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Auth;
using NutriScan.Application.Auth.Commands.Register;
using NutriScan.Application.Common.Exceptions;

namespace NutriScan.Application.UnitTests.Auth;

public class RegisterCommandHandlerTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();

    private RegisterCommandHandler CreateHandler() => new(_ctx.Db, _ctx.Hasher.Object, _ctx.Issuer);

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserWithNormalizedEmailAndHashedPassword()
    {
        // Arrange
        var command = new RegisterCommand("  Ana@Exemplu.RO ", "parola123", " Ana ", "Pop");

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        var user = await _ctx.Db.Users.SingleAsync();
        user.Email.Should().Be("ana@exemplu.ro");
        user.PasswordHash.Should().Be("hashed:parola123");
        user.FirstName.Should().Be("Ana");
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsTokensAndPersistsRefreshTokenHash()
    {
        // Arrange
        var command = new RegisterCommand("ana@exemplu.ro", "parola123", "Ana", "Pop");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("rt-1");
        result.User.IsProfileComplete.Should().BeFalse();
        var stored = await _ctx.Db.RefreshTokens.SingleAsync();
        stored.TokenHash.Should().Be("h:rt-1");
        stored.ExpiresAtUtc.Should().Be(_ctx.Now.AddDays(7));
        stored.UserId.Should().Be(result.User.Id);
    }

    [Fact]
    public async Task Handle_EmailAlreadyUsed_ThrowsConflict()
    {
        // Arrange
        await _ctx.SeedUserAsync("ana@exemplu.ro");
        var command = new RegisterCommand("ANA@exemplu.ro", "parola123", "Ana", "Pop");

        // Act
        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage(AuthErrors.EmailTaken);
    }

    public void Dispose() => _ctx.Dispose();
}
