using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NutriScan.Application.Auth;
using NutriScan.Application.Auth.Commands.Refresh;
using NutriScan.Application.Common.Exceptions;

namespace NutriScan.Application.UnitTests.Auth;

public class RefreshCommandHandlerTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();

    private RefreshCommandHandler CreateHandler() =>
        new(_ctx.Db, _ctx.Tokens.Object, _ctx.Issuer, _ctx.Clock.Object, NullLogger<RefreshCommandHandler>.Instance);

    private Task<NutriScan.Application.Auth.Dtos.AuthResult> Refresh(string? token) =>
        CreateHandler().Handle(new RefreshCommand(token), CancellationToken.None);

    [Fact]
    public async Task Handle_ActiveToken_RotatesWithinSameFamily()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        var family = Guid.NewGuid();
        var old = await _ctx.SeedRefreshTokenAsync(user, "old", family);

        // Act
        var result = await Refresh("old");

        // Assert
        result.RefreshToken.Should().Be("rt-1");
        var replacement = await _ctx.Db.RefreshTokens.SingleAsync(t => t.TokenHash == "h:rt-1");
        replacement.FamilyId.Should().Be(family);
        old.RevokedAtUtc.Should().Be(_ctx.Now);
        old.ReplacedByTokenId.Should().Be(replacement.Id);
    }

    [Fact]
    public async Task Handle_RevokedToken_RevokesWholeFamilyAndThrows()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        var family = Guid.NewGuid();
        await _ctx.SeedRefreshTokenAsync(user, "t1", family, revoked: true);
        var t2 = await _ctx.SeedRefreshTokenAsync(user, "t2", family);
        var t3 = await _ctx.SeedRefreshTokenAsync(user, "t3", Guid.NewGuid());

        // Act
        var act = () => Refresh("t1");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage(AuthErrors.InvalidSession);
        t2.IsRevoked.Should().BeTrue();
        t3.IsActive(_ctx.Now).Should().BeTrue();
        (await _ctx.Db.RefreshTokens.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorized()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        await _ctx.SeedRefreshTokenAsync(user, "old", Guid.NewGuid(), createdAtUtc: _ctx.Now.AddDays(-8));

        // Act
        var act = () => Refresh("old");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage(AuthErrors.InvalidSession);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsUnauthorized()
    {
        // Act
        var act = () => Refresh("nu-exista");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage(AuthErrors.InvalidSession);
    }

    [Fact]
    public async Task Handle_NullToken_ThrowsUnauthorized()
    {
        // Act
        var act = () => Refresh(null);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage(AuthErrors.InvalidSession);
    }

    public void Dispose() => _ctx.Dispose();
}
