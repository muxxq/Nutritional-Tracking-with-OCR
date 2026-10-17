using FluentAssertions;
using FluentValidation.TestHelper;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Diary.Commands.AddDiaryEntry;
using NutriScan.Application.Profile.Commands.UpdateProfile;
using NutriScan.Application.Recipes;
using NutriScan.Application.Recipes.Commands.CreateRecipe;
using NutriScan.Application.Statistics.Dtos;
using NutriScan.Application.Statistics.Queries.GetStatistics;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Application.UnitTests.Diary;
using NutriScan.Application.UnitTests.Profile;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.UnitTests.Statistics;

public class GetStatisticsTests : IDisposable
{
    private static readonly DateOnly Monday = DiaryTestData.Today.AddDays(-10);

    private readonly AuthTestContext _ctx = new();
    private User _user = null!;
    private FoodItem _food = null!;

    private Guid Me => _user.Id;

    /// <summary>R1 profile (target 1700 kcal) and a food with 100 kcal per 100 g, so grams equal kcal.</summary>
    private async Task ArrangeAsync()
    {
        _user = await _ctx.SeedUserAsync();
        await new UpdateProfileCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(ProfileTestData.R1(Me), CancellationToken.None);
        _food = new FoodItem { Name = "Test", CreatedByUserId = Me, Calories = 100, Proteins = 5, Carbohydrates = 10, Fats = 2 };
        _ctx.Db.FoodItems.Add(_food);
        await _ctx.Db.SaveChangesAsync();
    }

    private Task LogAsync(DateOnly date, double grams, Guid? userId = null, MealType meal = MealType.Lunch) =>
        new AddDiaryEntryCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(DiaryTestData.Add(userId ?? Me, _food.Id, grams, meal: meal, date: date), CancellationToken.None);

    private Task<StatisticsDto> GetStats(DateOnly from, DateOnly to) =>
        new GetStatisticsQueryHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(new GetStatisticsQuery(Me, from, to), CancellationToken.None);

    [Fact]
    public async Task GetStatistics_Week_ReturnsEveryDayAndAveragesLoggedDaysOnly()
    {
        // Arrange: 1500 kcal on Monday (two meals), 1700 on Wednesday, 1900 on Friday
        await ArrangeAsync();
        await LogAsync(Monday, 1000, meal: MealType.Breakfast);
        await LogAsync(Monday, 500);
        await LogAsync(Monday.AddDays(2), 1700);
        await LogAsync(Monday.AddDays(4), 1900);

        // Act
        var stats = await GetStats(Monday, Monday.AddDays(6));

        // Assert
        stats.TotalDays.Should().Be(7);
        stats.LoggedDays.Should().Be(3);
        stats.Days.Select(d => d.Date).Should().Equal(Enumerable.Range(0, 7).Select(i => Monday.AddDays(i)));
        stats.Days.Select(d => d.IsLogged).Should().Equal(true, false, true, false, true, false, false);
        stats.Days[0].Totals.Calories.Should().Be(1500);
        stats.Days[0].Status.Should().Be(PlanDayStatus.Under);
        stats.Days[1].Totals.Should().Be(NutrientsDto.Zero);
        stats.Days[1].Status.Should().BeNull();
        stats.Average!.Calories.Should().Be(1700);
        stats.Targets.Calories.Should().Be(1700);
        stats.Deviation!.Calories.Should().Be(0);
        stats.CaloriesDeviationPercent.Should().Be(0);
        stats.StatusCounts.Should().Be(new DayStatusCounts(Under: 1, Within: 1, Over: 1));
        stats.IsProfileComplete.Should().BeTrue();
    }

    [Fact]
    public async Task GetStatistics_DeviationIsAverageMinusDisplayedTarget()
    {
        // Arrange
        await ArrangeAsync();
        await LogAsync(Monday, 1785);

        // Act
        var stats = await GetStats(Monday, Monday);

        // Assert
        stats.Deviation!.Calories.Should().Be(85);
        stats.CaloriesDeviationPercent.Should().Be(5);
        (stats.Average! - stats.Deviation).Rounded().Should().Be(stats.Targets);
    }

    [Fact]
    public async Task GetStatistics_NothingLogged_ReturnsNullsNotError()
    {
        // Arrange
        await ArrangeAsync();

        // Act
        var stats = await GetStats(Monday, Monday.AddDays(29));

        // Assert
        stats.TotalDays.Should().Be(30);
        stats.LoggedDays.Should().Be(0);
        stats.Average.Should().BeNull();
        stats.Deviation.Should().BeNull();
        stats.CaloriesDeviationPercent.Should().BeNull();
        stats.StatusCounts.Should().Be(new DayStatusCounts(0, 0, 0));
    }

    [Fact]
    public async Task GetStatistics_IgnoresOtherUsersAndDaysOutsideRange()
    {
        // Arrange
        await ArrangeAsync();
        var other = await _ctx.SeedUserAsync("ion@exemplu.ro");
        await LogAsync(Monday, 800);
        await LogAsync(Monday.AddDays(-1), 900);
        await LogAsync(Monday.AddDays(1), 900);
        _ctx.Db.DiaryEntries.Add(new DiaryEntry
        {
            UserId = other.Id, FoodItemId = _food.Id, ConsumedDate = Monday, MealType = MealType.Lunch,
            QuantityGrams = 5000, CalculatedCalories = 5000
        });
        await _ctx.Db.SaveChangesAsync();

        // Act
        var stats = await GetStats(Monday, Monday);

        // Assert
        stats.LoggedDays.Should().Be(1);
        stats.Average!.Calories.Should().Be(800);
    }

    [Fact]
    public async Task GetStatistics_CountsRecipeEntries()
    {
        // Arrange: the SRS recipe, 194.5 kcal per serving, logged as 2 servings
        await ArrangeAsync();
        var oats = DiaryTestData.Oats(Me);
        _ctx.Db.FoodItems.Add(oats);
        await _ctx.Db.SaveChangesAsync();
        var recipe = await new CreateRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(
            new CreateRecipeCommand(Me, "Ovăz", null, null, 4, null, [new(oats.Id, 200)]), CancellationToken.None);
        await new AddDiaryEntryCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(DiaryTestData.AddRecipe(Me, recipe.Id, servings: 2, date: Monday), CancellationToken.None);

        // Act
        var stats = await GetStats(Monday, Monday);

        // Assert
        stats.Days[0].Totals.Calories.Should().Be(389);
    }

    [Theory]
    [InlineData(0, 365, true)]
    [InlineData(0, 366, false)]
    [InlineData(1, 0, false)]
    public void Validator_RangeLength(int fromOffset, int toOffset, bool valid)
    {
        // Arrange
        var validator = new GetStatisticsQueryValidator(_ctx.Clock.Object);
        var query = new GetStatisticsQuery(Guid.NewGuid(), Monday.AddDays(-400 + fromOffset), Monday.AddDays(-400 + toOffset));

        // Act
        var result = validator.TestValidate(query);

        // Assert
        result.IsValid.Should().Be(valid);
    }

    [Fact]
    public void Validator_DatesOutOfBounds_AreRejected()
    {
        // Arrange
        var validator = new GetStatisticsQueryValidator(_ctx.Clock.Object);
        var today = DateOnly.FromDateTime(_ctx.Now);

        // Act
        var tooEarly = validator.TestValidate(new GetStatisticsQuery(Guid.NewGuid(), new DateOnly(1999, 12, 31), new DateOnly(2000, 1, 5)));
        var tooLate = validator.TestValidate(new GetStatisticsQuery(Guid.NewGuid(), today, today.AddDays(367)));

        // Assert
        tooEarly.ShouldHaveValidationErrorFor(q => q.From);
        tooLate.ShouldHaveValidationErrorFor(q => q.To);
    }

    public void Dispose() => _ctx.Dispose();
}
