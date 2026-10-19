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
    public async Task Handle_TokenRotatedConcurrently_RevokesFamilyAndThrows()
    {
        // Arrange: this request has read RT1 as active...
        var user = await _ctx.SeedUserAsync();
        var family = Guid.NewGuid();
        await _ctx.SeedRefreshTokenAsync(user, "rt1", family);
        await _ctx.Db.RefreshTokens.SingleAsync(t => t.TokenHash == "h:rt1");

        // ...while a concurrent request rotates RT1 and commits first.
        await using (var other = _ctx.CreateContext())
        {
            var issuer = new AuthSessionIssuer(other, _ctx.Tokens.Object, _ctx.Clock.Object);
            await new RefreshCommandHandler(other, _ctx.Tokens.Object, issuer, _ctx.Clock.Object, NullLogger<RefreshCommandHandler>.Instance)
                .Handle(new RefreshCommand("rt1"), CancellationToken.None);
        }

        // Act
        var act = () => Refresh("rt1");

        // Assert: no second live child; the whole family is revoked.
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage(AuthErrors.InvalidSession);
        await using var verify = _ctx.CreateContext();
        var tokens = await verify.RefreshTokens.Where(t => t.FamilyId == family).ToListAsync();
        tokens.Should().NotBeEmpty().And.OnlyContain(t => t.RevokedAtUtc != null);
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
