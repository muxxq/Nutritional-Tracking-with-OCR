using FluentAssertions;
using NutriScan.Application.Auth.Commands.Logout;

namespace NutriScan.Application.UnitTests.Auth;

public class LogoutCommandHandlerTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();

    private Task Logout(string? token) =>
        new LogoutCommandHandler(_ctx.Db, _ctx.Tokens.Object, _ctx.Clock.Object)
            .Handle(new LogoutCommand(token), CancellationToken.None);

    [Fact]
    public async Task Handle_KnownToken_RevokesOnlyItsFamily()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        var family = Guid.NewGuid();
        await _ctx.SeedRefreshTokenAsync(user, "older", family, revoked: true);
        var current = await _ctx.SeedRefreshTokenAsync(user, "current", family);
        var otherDevice = await _ctx.SeedRefreshTokenAsync(user, "other", Guid.NewGuid());

        // Act
        await Logout("current");

        // Assert
        current.IsRevoked.Should().BeTrue();
        otherDevice.IsActive(_ctx.Now).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("nu-exista")]
    public async Task Handle_NullOrUnknownToken_DoesNothing(string? token)
    {
        // Act
        var act = () => Logout(token);

        // Assert
        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _ctx.Dispose();
}
