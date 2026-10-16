using FluentAssertions;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Application.Diary.Queries.GetRecentFoods;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Diary;

public class GetRecentFoodsTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();
    private readonly Guid _me = Guid.NewGuid();

    private async Task<FoodItem> AddFoodAsync(FoodItem food)
    {
        _ctx.Db.FoodItems.Add(food);
        await _ctx.Db.SaveChangesAsync();
        return food;
    }

    private async Task LogAsync(Guid userId, FoodItem food, double grams, double? servings = null)
    {
        var entry = new DiaryEntry
        {
            UserId = userId, FoodItemId = food.Id, ConsumedDate = DiaryTestData.Today, MealType = MealType.Lunch,
            QuantityGrams = grams, Servings = servings
        };
        entry.RecalculateNutrients(food);
        _ctx.Db.DiaryEntries.Add(entry);
        await _ctx.Db.SaveChangesAsync();
        _ctx.Now = _ctx.Now.AddMinutes(1);
    }

    private Task<IReadOnlyList<RecentFoodDto>> Recent(Guid userId, int limit = 10) =>
        new GetRecentFoodsQueryHandler(_ctx.Db).Handle(new GetRecentFoodsQuery(userId, limit), CancellationToken.None);

    [Fact]
    public async Task Recent_DistinctByFood_NewestFirst_WithLastQuantity()
    {
        // Arrange
        var oats = await AddFoodAsync(DiaryTestData.Oats(_me));
        var chicken = await AddFoodAsync(DiaryTestData.Chicken());
        await LogAsync(_me, oats, 100);
        await LogAsync(_me, chicken, 150);
        var lastOatsAt = _ctx.Now;
        await LogAsync(_me, oats, 60, servings: 1.5);

        // Act
        var recent = await Recent(_me);

        // Assert
        recent.Select(r => r.Food.Id).Should().Equal(oats.Id, chicken.Id);
        recent[0].LastQuantityGrams.Should().Be(60);
        recent[0].LastServings.Should().Be(1.5);
        recent[0].LastUsedAtUtc.Should().Be(lastOatsAt);
        recent[1].LastQuantityGrams.Should().Be(150);
    }

    [Fact]
    public async Task Recent_ExcludesArchivedFoods()
    {
        // Arrange
        var oats = await AddFoodAsync(DiaryTestData.Oats(_me));
        var chicken = await AddFoodAsync(DiaryTestData.Chicken());
        await LogAsync(_me, chicken, 100);
        await LogAsync(_me, oats, 100);
        oats.Archive(_ctx.Now);
        await _ctx.Db.SaveChangesAsync();

        // Act
        var recent = await Recent(_me);

        // Assert
        recent.Select(r => r.Food.Id).Should().Equal(chicken.Id);
    }

    [Fact]
    public async Task Recent_RespectsLimit()
    {
        // Arrange
        for (var i = 0; i < 3; i++)
        {
            await LogAsync(_me, await AddFoodAsync(DiaryTestData.Chicken()), 100);
        }

        // Act
        var recent = await Recent(_me, limit: 2);

        // Assert
        recent.Should().HaveCount(2);
    }

    [Fact]
    public async Task Recent_OnlyOwnEntries()
    {
        // Arrange
        var chicken = await AddFoodAsync(DiaryTestData.Chicken());
        await LogAsync(Guid.NewGuid(), chicken, 100);

        // Act
        var recent = await Recent(_me);

        // Assert
        recent.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(31, false)]
    [InlineData(1, true)]
    [InlineData(30, true)]
    public void Validate_LimitBounds(int limit, bool expected) =>
        new GetRecentFoodsQueryValidator().Validate(new GetRecentFoodsQuery(_me, limit)).IsValid.Should().Be(expected);

    public void Dispose() => _ctx.Dispose();
}
