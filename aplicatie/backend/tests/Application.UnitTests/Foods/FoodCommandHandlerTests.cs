using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Foods.Commands.ArchiveFood;
using NutriScan.Application.Foods.Commands.CreateFood;
using NutriScan.Application.Foods.Commands.UpdateFood;
using NutriScan.Application.Foods.Queries.GetFood;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Foods;

public class FoodCommandHandlerTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();
    private readonly Guid _me = Guid.NewGuid();

    private async Task<FoodItem> AddAsync(FoodItem food)
    {
        _ctx.Db.FoodItems.Add(food);
        await _ctx.Db.SaveChangesAsync();
        return food;
    }

    private Task Archive(Guid foodId) =>
        new ArchiveFoodCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(new ArchiveFoodCommand(_me, foodId), CancellationToken.None);

    private Task<NutriScan.Application.Foods.Dtos.FoodDto> Update(Guid foodId, string name = "Ovăz nou") =>
        new UpdateFoodCommandHandler(_ctx.Db).Handle(
            UpdateFoodCommand.From(foodId, FoodTestData.Oats(_me) with { Name = name }), CancellationToken.None);

    [Fact]
    public async Task Create_SetsOwnerAndUnverified()
    {
        // Act
        var dto = await new CreateFoodCommandHandler(_ctx.Db).Handle(FoodTestData.Oats(_me), CancellationToken.None);

        // Assert
        dto.IsOwn.Should().BeTrue();
        dto.IsVerified.Should().BeFalse();
        dto.Per100g.SaltGrams.Should().Be(0.01);
        var stored = await _ctx.Db.FoodItems.SingleAsync();
        stored.CreatedByUserId.Should().Be(_me);
        stored.SearchText.Should().Be("fulgi de ovaz generic");
    }

    [Fact]
    public async Task Create_InconsistentEnergy_ReturnsWarning()
    {
        // Act
        var dto = await new CreateFoodCommandHandler(_ctx.Db).Handle(
            FoodTestData.Oats(_me) with { Calories = 250, Proteins = 10, Carbohydrates = 30, Sugars = null, Fats = 5, SaturatedFats = null },
            CancellationToken.None);

        // Assert
        dto.EnergyWarning.Should().Contain("18%");
    }

    [Fact]
    public async Task Get_OtherUsersFood_ThrowsNotFound()
    {
        // Arrange
        var food = await AddAsync(FoodTestData.Food("Secret", Guid.NewGuid()));

        // Act
        var act = () => new GetFoodQueryHandler(_ctx.Db).Handle(new GetFoodQuery(_me, food.Id), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Update_OwnFood_ReplacesFields()
    {
        // Arrange
        var food = await AddAsync(FoodTestData.Food("Vechi", _me));

        // Act
        var dto = await Update(food.Id, "Ovăz nou");

        // Assert
        dto.Name.Should().Be("Ovăz nou");
        food.SearchText.Should().Be("ovaz nou generic");
        food.Calories.Should().Be(389);
    }

    [Fact]
    public async Task Create_WithoutCategory_IsOther()
    {
        // Act
        var dto = await new CreateFoodCommandHandler(_ctx.Db).Handle(FoodTestData.Oats(_me), CancellationToken.None);

        // Assert
        dto.Category.Should().Be(FoodCategory.Other);
        (await _ctx.Db.FoodItems.SingleAsync()).Category.Should().Be(FoodCategory.Other);
    }

    [Fact]
    public async Task Create_WithCategory_Stores()
    {
        // Act
        var dto = await new CreateFoodCommandHandler(_ctx.Db).Handle(
            FoodTestData.Oats(_me) with { Category = FoodCategory.Dairy }, CancellationToken.None);

        // Assert
        dto.Category.Should().Be(FoodCategory.Dairy);
    }

    [Fact]
    public async Task Update_ChangesCategory()
    {
        // Arrange
        var food = await AddAsync(FoodTestData.Food("Vechi", _me));

        // Act
        var dto = await new UpdateFoodCommandHandler(_ctx.Db).Handle(
            UpdateFoodCommand.From(food.Id, FoodTestData.Oats(_me) with { Category = FoodCategory.FruitsVegetables }),
            CancellationToken.None);

        // Assert
        dto.Category.Should().Be(FoodCategory.FruitsVegetables);
        food.Category.Should().Be(FoodCategory.FruitsVegetables);
    }

    [Fact]
    public async Task Update_GlobalFood_ThrowsForbidden()
    {
        // Arrange
        var food = await AddAsync(FoodTestData.Food("Global", verified: true));

        // Act
        var act = () => Update(food.Id);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Update_ArchivedOwnFood_ThrowsNotFound()
    {
        // Arrange
        var food = FoodTestData.Food("Arhivat", _me);
        food.Archive(_ctx.Now);
        await AddAsync(food);

        // Act
        var act = () => Update(food.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Archive_OwnFood_SetsArchivedAt()
    {
        // Arrange
        var food = await AddAsync(FoodTestData.Food("Al meu", _me));

        // Act
        await Archive(food.Id);

        // Assert
        food.ArchivedAtUtc.Should().Be(_ctx.Now);
    }

    [Fact]
    public async Task Archive_Twice_SecondThrowsNotFound()
    {
        // Arrange
        var food = await AddAsync(FoodTestData.Food("Al meu", _me));
        await Archive(food.Id);

        // Act
        var act = () => Archive(food.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Archive_GlobalFood_ThrowsForbidden()
    {
        // Arrange
        var food = await AddAsync(FoodTestData.Food("Global", verified: true));

        // Act
        var act = () => Archive(food.Id);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    public void Dispose() => _ctx.Dispose();
}
