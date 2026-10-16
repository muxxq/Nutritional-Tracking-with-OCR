using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.ShoppingList.Commands.AddShoppingListItem;
using NutriScan.Application.ShoppingList.Commands.ClearPurchasedItems;
using NutriScan.Application.ShoppingList.Commands.DeleteShoppingListItem;
using NutriScan.Application.ShoppingList.Commands.SetShoppingListItemPurchased;
using NutriScan.Application.ShoppingList.Queries.GetShoppingList;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.ShoppingList;

public class ShoppingListItemTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();
    private readonly Guid _me = Guid.NewGuid();

    private async Task<ShoppingListItem> SeedAsync(string name, bool purchased = false, Guid? userId = null)
    {
        var item = new ShoppingListItem { UserId = userId ?? _me, ItemName = name, IsPurchased = purchased };
        _ctx.Db.ShoppingListItems.Add(item);
        await _ctx.Db.SaveChangesAsync();
        return item;
    }

    private Task SetPurchased(Guid itemId, bool value, Guid? userId = null) =>
        new SetShoppingListItemPurchasedCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(new SetShoppingListItemPurchasedCommand(userId ?? _me, itemId, value), CancellationToken.None);

    private Task Delete(Guid itemId, Guid? userId = null) =>
        new DeleteShoppingListItemCommandHandler(_ctx.Db)
            .Handle(new DeleteShoppingListItemCommand(userId ?? _me, itemId), CancellationToken.None);

    [Fact]
    public async Task Add_Manual_TrimsAndDefaultsCategory()
    {
        // Act
        var dto = await new AddShoppingListItemCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(
            new AddShoppingListItemCommand(_me, "  Hârtie de copt  ", "  1 rolă ", null), CancellationToken.None);

        // Assert
        dto.Name.Should().Be("Hârtie de copt");
        dto.QuantityText.Should().Be("1 rolă");
        dto.QuantityGrams.Should().BeNull();
        dto.Category.Should().Be(FoodCategory.Other);
        dto.IsManual.Should().BeTrue();
        dto.IsPurchased.Should().BeFalse();
        var stored = await _ctx.Db.ShoppingListItems.SingleAsync();
        stored.UserId.Should().Be(_me);
        stored.CreatedAtUtc.Should().Be(_ctx.Now);
    }

    [Fact]
    public async Task Add_BlankQuantity_IsNull()
    {
        // Act
        var dto = await new AddShoppingListItemCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(
            new AddShoppingListItemCommand(_me, "Sare", "   ", FoodCategory.Pantry), CancellationToken.None);

        // Assert
        dto.QuantityText.Should().BeNull();
        dto.Category.Should().Be(FoodCategory.Pantry);
    }

    [Fact]
    public async Task SetPurchased_TogglesAndSetsUpdatedAt()
    {
        // Arrange
        var item = await SeedAsync("Lapte");
        _ctx.Now = _ctx.Now.AddMinutes(5);

        // Act
        await SetPurchased(item.Id, true);

        // Assert
        item.IsPurchased.Should().BeTrue();
        item.UpdatedAtUtc.Should().Be(_ctx.Now);
        await SetPurchased(item.Id, false);
        item.IsPurchased.Should().BeFalse();
    }

    [Fact]
    public async Task SetPurchased_OtherUsersItem_ThrowsNotFound()
    {
        // Arrange
        var item = await SeedAsync("Lapte", userId: Guid.NewGuid());

        // Act
        var act = () => SetPurchased(item.Id, true);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        item.IsPurchased.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_OwnItem_Removes()
    {
        // Arrange
        var item = await SeedAsync("Lapte");

        // Act
        await Delete(item.Id);

        // Assert
        (await _ctx.Db.ShoppingListItems.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_OtherUsersItem_ThrowsNotFound()
    {
        // Arrange
        var item = await SeedAsync("Lapte", userId: Guid.NewGuid());

        // Act
        var act = () => Delete(item.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ClearPurchased_RemovesOnlyOwnPurchased()
    {
        // Arrange
        await SeedAsync("Cumpărat", purchased: true);
        await SeedAsync("De cumpărat");
        await SeedAsync("Al altcuiva", purchased: true, userId: Guid.NewGuid());

        // Act
        await new ClearPurchasedItemsCommandHandler(_ctx.Db).Handle(new ClearPurchasedItemsCommand(_me), CancellationToken.None);

        // Assert
        (await _ctx.Db.ShoppingListItems.Select(i => i.ItemName).ToListAsync())
            .Should().BeEquivalentTo("De cumpărat", "Al altcuiva");
    }

    [Fact]
    public async Task GetShoppingList_CountsPurchased()
    {
        // Arrange
        await SeedAsync("A", purchased: true);
        await SeedAsync("B");
        await SeedAsync("C", userId: Guid.NewGuid());

        // Act
        var list = await new GetShoppingListQueryHandler(_ctx.Db).Handle(new GetShoppingListQuery(_me), CancellationToken.None);

        // Assert
        list.TotalCount.Should().Be(2);
        list.PurchasedCount.Should().Be(1);
        list.Categories.Should().ContainSingle().Which.Items.Select(i => i.Name).Should().Equal("A", "B");
    }

    private static readonly AddShoppingListItemCommandValidator Validator = new();

    [Fact]
    public void Validate_BlankName_IsInvalid() =>
        Validator.Validate(new AddShoppingListItemCommand(Guid.NewGuid(), "   ", null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_Name201_IsInvalid() =>
        Validator.Validate(new AddShoppingListItemCommand(Guid.NewGuid(), new string('a', 201), null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_Quantity51_IsInvalid() =>
        Validator.Validate(new AddShoppingListItemCommand(Guid.NewGuid(), "Lapte", new string('1', 51), null)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_UnknownCategory_IsInvalid() =>
        Validator.Validate(new AddShoppingListItemCommand(Guid.NewGuid(), "Lapte", null, (FoodCategory)99)).IsValid.Should().BeFalse();

    public void Dispose() => _ctx.Dispose();
}
