using FluentAssertions;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Application.Diary.Queries.GetDiaryDay;
using NutriScan.Application.Profile.Commands.UpdateProfile;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Application.UnitTests.Profile;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.UnitTests.Diary;

public class GetDiaryDayTests : IDisposable
{
    private static readonly NutrientsDto R1Targets = new(1700, 124, 195, 47);

    private readonly AuthTestContext _ctx = new();

    private async Task<User> SeedR1UserAsync(string email = "ana@exemplu.ro")
    {
        var user = await _ctx.SeedUserAsync(email);
        await new UpdateProfileCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(ProfileTestData.R1(user.Id), CancellationToken.None);
        return user;
    }

    private async Task<FoodItem> AddFoodAsync(FoodItem food)
    {
        _ctx.Db.FoodItems.Add(food);
        await _ctx.Db.SaveChangesAsync();
        return food;
    }

    private async Task LogAsync(Guid userId, FoodItem food, double grams, MealType meal, DateOnly? date = null)
    {
        var entry = new DiaryEntry
        {
            UserId = userId, FoodItemId = food.Id, ConsumedDate = date ?? DiaryTestData.Today, MealType = meal, QuantityGrams = grams
        };
        entry.RecalculateNutrients(food);
        _ctx.Db.DiaryEntries.Add(entry);
        await _ctx.Db.SaveChangesAsync();
        _ctx.Now = _ctx.Now.AddMinutes(1);
    }

    private Task<DiaryDayDto> GetDay(Guid userId, DateOnly? date = null) =>
        new GetDiaryDayQueryHandler(_ctx.Db, _ctx.Clock.Object).Handle(
            new GetDiaryDayQuery(userId, date ?? DiaryTestData.Today), CancellationToken.None);

    [Fact]
    public async Task GetDay_Empty_ReturnsFourEmptyMealsAndZeroTotals()
    {
        // Arrange
        var user = await SeedR1UserAsync();

        // Act
        var day = await GetDay(user.Id);

        // Assert
        day.Date.Should().Be(DiaryTestData.Today);
        day.Meals.Select(m => m.MealType).Should().Equal(MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snack);
        day.Meals.Should().OnlyContain(m => m.Entries.Count == 0 && m.Totals == NutrientsDto.Zero);
        day.Totals.Should().Be(NutrientsDto.Zero);
        day.Targets.Should().Be(R1Targets);
        day.Remaining.Should().Be(R1Targets);
        day.IsProfileComplete.Should().BeTrue();
    }

    [Fact]
    public async Task GetDay_SumsMealsAndDay()
    {
        // Arrange
        var user = await SeedR1UserAsync();
        var oats = await AddFoodAsync(DiaryTestData.Oats(user.Id));
        var chicken = await AddFoodAsync(DiaryTestData.Chicken());
        await LogAsync(user.Id, oats, 60, MealType.Breakfast);
        await LogAsync(user.Id, oats, 40, MealType.Breakfast);
        await LogAsync(user.Id, chicken, 200, MealType.Dinner);

        // Act
        var day = await GetDay(user.Id);

        // Assert
        var breakfast = day.Meals[0];
        breakfast.Entries.Select(e => e.QuantityGrams).Should().Equal(60, 40);
        breakfast.Totals.Should().Be(new NutrientsDto(389, 16.9, 66.3, 6.9));
        day.Meals[1].Entries.Should().BeEmpty();
        day.Meals[2].Totals.Should().Be(new NutrientsDto(240, 45, 0, 5.2));
        day.Totals.Should().Be(new NutrientsDto(629, 61.9, 66.3, 12.1));
        day.Remaining.Should().Be(new NutrientsDto(1071, 62.1, 128.7, 34.9));
    }

    [Fact]
    public async Task GetDay_SumsBeforeRounding()
    {
        // Arrange: each entry has 0.04 g protein, displayed as 0.0
        var user = await SeedR1UserAsync();
        var lowProtein = await AddFoodAsync(new FoodItem { Name = "Castravete", Calories = 15, Proteins = 4, Carbohydrates = 0, Fats = 0 });
        for (var i = 0; i < 3; i++)
        {
            await LogAsync(user.Id, lowProtein, 1, MealType.Snack);
        }

        // Act
        var day = await GetDay(user.Id);

        // Assert
        day.Meals[3].Entries.Should().OnlyContain(e => e.Nutrients.Proteins == 0);
        day.Totals.Proteins.Should().Be(0.1);
    }

    [Fact]
    public async Task GetDay_RemainingAddsUpWithDisplayedTotals()
    {
        // Arrange: 2.25 g fat rounds up to 2.3, so the remaining 44.75 must show 44.7, not 44.8
        var user = await SeedR1UserAsync();
        var food = await AddFoodAsync(new FoodItem { Name = "Iaurt", Calories = 60, Proteins = 4, Carbohydrates = 5, Fats = 2.25 });
        await LogAsync(user.Id, food, 100, MealType.Snack);

        // Act
        var day = await GetDay(user.Id);

        // Assert
        day.Totals.Fats.Should().Be(2.3);
        day.Remaining.Fats.Should().Be(44.7);
    }

    [Fact]
    public async Task GetDay_IgnoresOtherDaysAndOtherUsers()
    {
        // Arrange
        var user = await SeedR1UserAsync();
        var other = await _ctx.SeedUserAsync("ion@exemplu.ro");
        var chicken = await AddFoodAsync(DiaryTestData.Chicken());
        await LogAsync(user.Id, chicken, 100, MealType.Lunch);
        await LogAsync(user.Id, chicken, 100, MealType.Lunch, DiaryTestData.Today.AddDays(-1));
        await LogAsync(user.Id, chicken, 100, MealType.Lunch, DiaryTestData.Today.AddDays(1));
        await LogAsync(other.Id, chicken, 100, MealType.Lunch);

        // Act
        var day = await GetDay(user.Id);

        // Assert
        day.Meals[1].Entries.Should().ContainSingle();
        day.Totals.Calories.Should().Be(120);
    }

    [Fact]
    public async Task GetDay_EntryWithArchivedFood_IsListedAsArchived()
    {
        // Arrange
        var user = await SeedR1UserAsync();
        var oats = await AddFoodAsync(DiaryTestData.Oats(user.Id));
        await LogAsync(user.Id, oats, 100, MealType.Breakfast);
        oats.Archive(_ctx.Now);
        await _ctx.Db.SaveChangesAsync();

        // Act
        var day = await GetDay(user.Id);

        // Assert
        day.Meals[0].Entries.Should().ContainSingle().Which.Food.IsArchived.Should().BeTrue();
        day.Totals.Calories.Should().Be(389);
    }

    [Fact]
    public async Task GetDay_IncompleteProfile_ReturnsDefaultTargets()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        var defaults = NutritionTargetCalculator.CalculateBestEffort(user, DiaryTestData.Today);

        // Act
        var day = await GetDay(user.Id);

        // Assert
        day.IsProfileComplete.Should().BeFalse();
        day.Targets.Should().Be(new NutrientsDto(
            defaults.Calories.Value, defaults.ProteinsGrams.Value, defaults.CarbsGrams.Value, defaults.FatsGrams.Value));
    }

    public void Dispose() => _ctx.Dispose();
}
