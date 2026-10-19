using FluentAssertions;
using NutriScan.Application.Auth;
using NutriScan.Application.Auth.Queries.GetCurrentUser;
using NutriScan.Application.Common.Exceptions;

namespace NutriScan.Application.UnitTests.Auth;

public class GetCurrentUserQueryHandlerTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();

    [Fact]
    public async Task Handle_ExistingUser_ReturnsDto()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();

        // Act
        var dto = await new GetCurrentUserQueryHandler(_ctx.Db).Handle(new GetCurrentUserQuery(user.Id), CancellationToken.None);

        // Assert
        dto.Id.Should().Be(user.Id);
        dto.Email.Should().Be("ana@exemplu.ro");
        dto.IsProfileComplete.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_MissingUser_ThrowsUnauthorized()
    {
        // Act
        var act = () => new GetCurrentUserQueryHandler(_ctx.Db).Handle(new GetCurrentUserQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage(AuthErrors.InvalidSession);
    }

    public void Dispose() => _ctx.Dispose();
}
