using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.ShoppingList;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.UnitTests.ShoppingList;

/// <summary>A double tap or another tab: a concurrent request deletes the line between this request's read and write.</summary>
public class ShoppingListConcurrencyTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();

    /// <summary>This request has loaded a line and marked it for <paramref name="change"/>; another request deletes it first.</summary>
    private async Task ArrangeRaceAsync(Action<ShoppingListItem> change)
    {
        var item = new ShoppingListItem { UserId = Guid.NewGuid(), ItemName = "Lapte", IsPurchased = true };
        _ctx.Db.ShoppingListItems.Add(item);
        await _ctx.Db.SaveChangesAsync();
        change(item);

        await using var other = _ctx.CreateContext();
        other.ShoppingListItems.Remove(await other.ShoppingListItems.SingleAsync());
        await other.SaveChangesAsync();
    }

    [Fact]
    public async Task SaveOrNotFound_ItemDeletedConcurrently_ThrowsNotFound()
    {
        // Arrange
        await ArrangeRaceAsync(item => item.IsPurchased = false);

        // Act
        var act = () => _ctx.Db.SaveOrNotFoundAsync(CancellationToken.None);

        // Assert: 404 like any other missing line, not a 500
        await act.Should().ThrowAsync<NotFoundException>().WithMessage(ShoppingListErrors.NotFound);
    }

    [Fact]
    public async Task SaveDeletes_AlreadyDeletedConcurrently_Succeeds()
    {
        // Arrange: "clear purchased" pressed twice
        await ArrangeRaceAsync(item => _ctx.Db.ShoppingListItems.Remove(item));

        // Act
        var act = () => _ctx.Db.SaveDeletesAsync(CancellationToken.None);

        // Assert: the lines are gone, which is what both requests wanted
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveOrConflict_GeneratedConcurrently_ThrowsConflict()
    {
        // Arrange: "Generate" pressed twice; both requests replace the same generated lines
        await ArrangeRaceAsync(item => _ctx.Db.ShoppingListItems.Remove(item));

        // Act
        var act = () => _ctx.Db.SaveOrConflictAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage(ShoppingListErrors.ConcurrentChange);
    }

    public void Dispose() => _ctx.Dispose();
}
