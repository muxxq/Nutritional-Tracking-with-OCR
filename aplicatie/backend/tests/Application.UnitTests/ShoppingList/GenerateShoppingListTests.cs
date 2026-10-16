using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.ShoppingList;
using NutriScan.Application.ShoppingList.Commands.GenerateShoppingList;
using NutriScan.Application.ShoppingList.Dtos;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;
using static NutriScan.Application.UnitTests.ShoppingList.ShoppingListTestContext;

namespace NutriScan.Application.UnitTests.ShoppingList;

public class GenerateShoppingListTests : IDisposable
{
    private readonly ShoppingListTestContext _t = new();

    private Task<ShoppingListDto> Generate(DateOnly? from = null, DateOnly? to = null, Guid? userId = null) =>
        new GenerateShoppingListCommandHandler(_t.Ctx.Db, _t.Ctx.Clock.Object).Handle(
            new GenerateShoppingListCommand(userId ?? _t.Me, from ?? Monday, to ?? Monday.AddDays(6)), CancellationToken.None);

    private static IEnumerable<ShoppingListItemDto> Items(ShoppingListDto list) => list.Categories.SelectMany(c => c.Items);

    [Fact]
    public async Task Generate_SrsExample_OneChickenItem400g()
    {
        // Arrange: FR-12 acceptance criterion
        var chicken = await _t.FoodAsync("Piept de pui", FoodCategory.MeatFish);
        await _t.PlanRecipeAsync(await _t.RecipeAsync(1, (chicken.Id, 150)), 1);
        await _t.PlanRecipeAsync(await _t.RecipeAsync(1, (chicken.Id, 250)), 1);

        // Act
        var list = await Generate();

        // Assert
        var item = Items(list).Should().ContainSingle().Subject;
        item.Name.Should().Be("Piept de pui");
        item.Category.Should().Be(FoodCategory.MeatFish);
        item.QuantityGrams.Should().Be(400);
        item.IsManual.Should().BeFalse();
        item.FoodId.Should().Be(chicken.Id);
        list.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Generate_GroupsByCategoryInEnumOrder()
    {
        // Arrange
        var chicken = await _t.FoodAsync("Piept de pui", FoodCategory.MeatFish);
        var oats = await _t.FoodAsync("Ovăz", FoodCategory.Grains);
        var banana = await _t.FoodAsync("Banană", FoodCategory.FruitsVegetables);
        foreach (var food in new[] { chicken, oats, banana })
        {
            await _t.PlanFoodAsync(food.Id, 100);
        }

        // Act
        var list = await Generate();

        // Assert
        list.Categories.Select(c => c.Category)
            .Should().Equal(FoodCategory.FruitsVegetables, FoodCategory.MeatFish, FoodCategory.Grains);
    }

    [Fact]
    public async Task Generate_Twice_ReplacesGeneratedKeepsManual()
    {
        // Arrange
        var oats = await _t.FoodAsync("Ovăz", FoodCategory.Grains);
        var rice = await _t.FoodAsync("Orez", FoodCategory.Grains);
        var oatsMeal = await _t.PlanFoodAsync(oats.Id, 100);
        await Generate();
        _t.Ctx.Db.ShoppingListItems.Add(new ShoppingListItem
        {
            UserId = _t.Me, ItemName = "Hârtie de copt", QuantityText = "1 rolă", IsPurchased = true
        });
        _t.Ctx.Db.PlannedMeals.Remove(await _t.Ctx.Db.PlannedMeals.SingleAsync(m => m.Id == oatsMeal));
        await _t.Ctx.Db.SaveChangesAsync();
        await _t.PlanFoodAsync(rice.Id, 200);

        // Act
        var list = await Generate();

        // Assert
        Items(list).Select(i => i.Name).Should().BeEquivalentTo("Orez", "Hârtie de copt");
        Items(list).Single(i => i.IsManual).IsPurchased.Should().BeTrue();
        (await _t.Ctx.Db.ShoppingListItems.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Generate_ExcludesEatenMeals()
    {
        // Arrange
        var oats = await _t.FoodAsync("Ovăz", FoodCategory.Grains);
        var rice = await _t.FoodAsync("Orez", FoodCategory.Grains);
        var eaten = await _t.PlanFoodAsync(oats.Id, 100);
        await _t.PlanFoodAsync(rice.Id, 200);
        var entry = new DiaryEntry
        {
            UserId = _t.Me, FoodItemId = oats.Id, ConsumedDate = Monday, MealType = MealType.Dinner, QuantityGrams = 100
        };
        _t.Ctx.Db.DiaryEntries.Add(entry);
        (await _t.Ctx.Db.PlannedMeals.SingleAsync(m => m.Id == eaten)).DiaryEntryId = entry.Id;
        await _t.Ctx.Db.SaveChangesAsync();

        // Act
        var list = await Generate();

        // Assert
        Items(list).Select(i => i.Name).Should().Equal("Orez");
    }

    [Fact]
    public async Task Generate_NothingPlanned_ThrowsAndKeepsList()
    {
        // Arrange
        var oats = await _t.FoodAsync("Ovăz", FoodCategory.Grains);
        await _t.PlanFoodAsync(oats.Id, 100);
        await Generate();

        // Act
        var act = () => Generate(Monday.AddDays(7), Monday.AddDays(13));

        // Assert
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().ContainSingle(e => e.ErrorMessage == ShoppingListErrors.NothingPlanned);
        (await _t.Ctx.CreateContext().ShoppingListItems.SingleAsync()).ItemName.Should().Be("Ovăz");
    }

    [Fact]
    public async Task Generate_ArchivedFoodInPlan_IsIncluded()
    {
        // Arrange
        var oats = await _t.FoodAsync("Ovăz", FoodCategory.Grains, owner: _t.Me);
        await _t.PlanFoodAsync(oats.Id, 100);
        oats.Archive(_t.Ctx.Now);
        await _t.Ctx.Db.SaveChangesAsync();

        // Act
        var list = await Generate();

        // Assert
        Items(list).Should().ContainSingle().Which.Name.Should().Be("Ovăz");
    }

    [Fact]
    public async Task Generate_RoundsGramsUp()
    {
        // Arrange: 100 g for 3 servings, 1 planned → 33.33 g
        var oats = await _t.FoodAsync("Ovăz", FoodCategory.Grains);
        await _t.PlanRecipeAsync(await _t.RecipeAsync(3, (oats.Id, 100)), 1);

        // Act
        var list = await Generate();

        // Assert
        Items(list).Single().QuantityGrams.Should().Be(34);
        (await _t.Ctx.Db.ShoppingListItems.SingleAsync()).QuantityGrams.Should().BeApproximately(33.333, 0.001);
    }

    [Fact]
    public async Task Generate_BatchCookedRecipe_DoesNotRoundFloatNoiseUp()
    {
        // Arrange: a 6-serving batch eaten one serving a day; six times 400/6 sums to 400.00000000000006 in doubles
        var chicken = await _t.FoodAsync("Piept de pui", FoodCategory.MeatFish);
        var recipeId = await _t.RecipeAsync(6, (chicken.Id, 400));
        for (var day = 0; day < 6; day++)
        {
            await _t.PlanRecipeAsync(recipeId, 1, Monday.AddDays(day));
        }

        // Act
        var list = await Generate();

        // Assert
        Items(list).Single().QuantityGrams.Should().Be(400);
    }

    [Fact]
    public async Task Generate_IgnoresOtherUsersMealsAndOutOfRange()
    {
        // Arrange
        var oats = await _t.FoodAsync("Ovăz", FoodCategory.Grains);
        var rice = await _t.FoodAsync("Orez", FoodCategory.Grains);
        await _t.PlanFoodAsync(oats.Id, 100, userId: Guid.NewGuid());
        await _t.PlanFoodAsync(oats.Id, 100, date: Monday.AddDays(7));
        await _t.PlanFoodAsync(rice.Id, 200);

        // Act
        var list = await Generate();

        // Assert
        Items(list).Select(i => i.Name).Should().Equal("Orez");
    }

    private static GenerateShoppingListCommandValidator Validator()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc));
        return new GenerateShoppingListCommandValidator(clock.Object);
    }

    [Fact]
    public void Validate_Range32Days_IsInvalid() =>
        Validator().Validate(new GenerateShoppingListCommand(Guid.NewGuid(), Monday, Monday.AddDays(31))).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_FromAfterTo_IsInvalid() =>
        Validator().Validate(new GenerateShoppingListCommand(Guid.NewGuid(), Monday, Monday.AddDays(-1))).IsValid.Should().BeFalse();

    public void Dispose() => _t.Dispose();
}
