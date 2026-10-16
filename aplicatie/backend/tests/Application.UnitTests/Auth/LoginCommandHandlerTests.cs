using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NutriScan.Application.Auth;
using NutriScan.Application.Auth.Commands.Login;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Models;

namespace NutriScan.Application.UnitTests.Auth;

public class LoginCommandHandlerTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();

    private LoginCommandHandler CreateHandler() => new(_ctx.Db, _ctx.Hasher.Object, _ctx.Issuer, _ctx.Clock.Object);

    private Task<NutriScan.Application.Auth.Dtos.AuthResult> Login(string email = "ana@exemplu.ro", string password = "parola123") =>
        CreateHandler().Handle(new LoginCommand(email, password), CancellationToken.None);

    private async Task FailLoginAsync(int times)
    {
        for (var i = 0; i < times; i++)
        {
            await Login(password: "gresita").Invoking(t => t).Should().ThrowAsync<UnauthorizedException>();
        }
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokensAndStartsNewFamily()
    {
        // Arrange
        await _ctx.SeedUserAsync();

        // Act
        var first = await Login();
        var second = await Login();

        // Assert
        first.AccessToken.Should().Be("access");
        second.RefreshToken.Should().Be("rt-2");
        var families = await _ctx.Db.RefreshTokens.Select(t => t.FamilyId).ToListAsync();
        families.Should().HaveCount(2).And.OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_EmailWithDifferentCaseAndSpaces_LogsIn()
    {
        // Arrange
        await _ctx.SeedUserAsync();

        // Act
        var result = await Login(email: "  ANA@exemplu.ro ");

        // Assert
        result.User.Email.Should().Be("ana@exemplu.ro");
    }

    [Fact]
    public async Task Handle_UnknownEmail_VerifiesDummyHashAndThrowsUnauthorized()
    {
        // Act
        var act = () => Login(email: "nimeni@exemplu.ro");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage(AuthErrors.InvalidCredentials);
        _ctx.Hasher.Verify(h => h.Verify("hashed:dummy", "parola123"), Times.Once);
    }

    [Fact]
    public async Task Handle_WrongPassword_IncrementsFailedAttempts()
    {
        // Arrange
        await _ctx.SeedUserAsync();

        // Act
        await FailLoginAsync(1);

        // Assert
        (await _ctx.Db.Users.SingleAsync()).FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ConcurrentWrongPasswords_CountsEveryAttempt()
    {
        // Arrange: this request has read the user with 0 failures...
        await _ctx.SeedUserAsync();
        await _ctx.Db.Users.SingleAsync();

        // ...while a concurrent wrong-password request commits first.
        await using (var other = _ctx.CreateContext())
        {
            var issuer = new AuthSessionIssuer(other, _ctx.Tokens.Object, _ctx.Clock.Object);
            await new LoginCommandHandler(other, _ctx.Hasher.Object, issuer, _ctx.Clock.Object)
                .Handle(new LoginCommand("ana@exemplu.ro", "gresita"), CancellationToken.None)
                .Invoking(t => t).Should().ThrowAsync<UnauthorizedException>();
        }

        // Act
        await FailLoginAsync(1);

        // Assert: both failures are counted, none is lost.
        await using var verify = _ctx.CreateContext();
        (await verify.Users.SingleAsync()).FailedLoginAttempts.Should().Be(2);
    }

    [Fact]
    public async Task Handle_FifthWrongPassword_LocksAccount()
    {
        // Arrange
        await _ctx.SeedUserAsync();

        // Act
        await FailLoginAsync(5);

        // Assert
        (await _ctx.Db.Users.SingleAsync()).LockoutEndUtc.Should().Be(_ctx.Now.AddMinutes(5));
    }

    [Fact]
    public async Task Handle_LockedAccountWithCorrectPassword_ThrowsUnauthorizedWithoutVerifying()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        user.LockoutEndUtc = _ctx.Now.AddMinutes(1);
        await _ctx.Db.SaveChangesAsync();

        // Act
        var act = () => Login();

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage(AuthErrors.InvalidCredentials);
        _ctx.Hasher.Verify(h => h.Verify(user.PasswordHash, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LockedAccount_VerifiesDummyHashToKeepTimingConstant()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        user.LockoutEndUtc = _ctx.Now.AddMinutes(1);
        await _ctx.Db.SaveChangesAsync();

        // Act
        await Login().Invoking(t => t).Should().ThrowAsync<UnauthorizedException>();

        // Assert
        _ctx.Hasher.Verify(h => h.Verify("hashed:dummy", "parola123"), Times.Once);
    }

    [Fact]
    public async Task Handle_SuccessAfterFailures_ResetsCounter()
    {
        // Arrange
        await _ctx.SeedUserAsync();
        await FailLoginAsync(3);

        // Act
        await Login();

        // Assert
        (await _ctx.Db.Users.SingleAsync()).FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task Handle_RehashNeeded_UpdatesPasswordHash()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        user.PasswordHash = "old-format";
        await _ctx.Db.SaveChangesAsync();
        _ctx.Hasher.Setup(h => h.Verify("old-format", "parola123")).Returns(PasswordCheckResult.SuccessRehashNeeded);

        // Act
        await Login();

        // Assert
        _ctx.Hasher.Verify(h => h.Hash("parola123"), Times.Once);
        (await _ctx.Db.Users.SingleAsync()).PasswordHash.Should().Be("hashed:parola123");
    }

    public void Dispose() => _ctx.Dispose();
}
