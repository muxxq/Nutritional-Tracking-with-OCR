using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Application.Planner;
using NutriScan.Application.Planner.Commands.CreatePlannedMeal;
using NutriScan.Application.Planner.Commands.EatPlannedMeal;
using NutriScan.Application.Planner.Dtos;
using NutriScan.Application.Recipes.Commands.CreateRecipe;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Application.UnitTests.Diary;
using NutriScan.Application.UnitTests.Recipes;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Planner;

public class EatPlannedMealTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();
    private readonly Guid _me = Guid.NewGuid();

    private async Task<FoodItem> AddOatsAsync()
    {
        var oats = DiaryTestData.Oats(_me);
        _ctx.Db.FoodItems.Add(oats);
        await _ctx.Db.SaveChangesAsync();
        return oats;
    }

    private Task<PlannedMealDto> Plan(CreatePlannedMealCommand command) =>
        new CreatePlannedMealCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(command, CancellationToken.None);

    private Task<DiaryEntryDto> Eat(Guid mealId, Guid? userId = null) =>
        new EatPlannedMealCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(new EatPlannedMealCommand(userId ?? _me, mealId), CancellationToken.None);

    [Fact]
    public async Task Eat_Recipe_CreatesSnapshotEntry()
    {
        // Arrange
        var oats = await AddOatsAsync();
        var recipe = await new CreateRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(RecipeTestData.Create(_me, (oats.Id, 200)), CancellationToken.None);
        var meal = await Plan(PlannerTestData.Recipe(_me, recipe.Id, servings: 2, meal: MealType.Lunch));
        _ctx.Now = _ctx.Now.AddHours(2);

        // Act
        var dto = await Eat(meal.Id);

        // Assert
        dto.Kind.Should().Be(DiaryEntryKind.Recipe);
        dto.Servings.Should().Be(2);
        dto.Nutrients.Calories.Should().Be(389);
        dto.MealType.Should().Be(MealType.Lunch);
        dto.Date.Should().Be(PlannerTestData.Today);
        (await _ctx.Db.PlannedMeals.SingleAsync()).DiaryEntryId.Should().Be(dto.Id);
        var entry = await _ctx.Db.DiaryEntries.SingleAsync();
        entry.CreatedAtUtc.Should().Be(_ctx.Now);
        entry.UserId.Should().Be(_me);
    }

    [Fact]
    public async Task Eat_Food_CopiesGramsAndServings()
    {
        // Arrange
        var oats = await AddOatsAsync();
        var meal = await Plan(PlannerTestData.Food(_me, oats.Id, grams: null, servings: 1.5));

        // Act
        var dto = await Eat(meal.Id);

        // Assert
        dto.Kind.Should().Be(DiaryEntryKind.Food);
        dto.QuantityGrams.Should().Be(60);
        dto.Servings.Should().Be(1.5);
        dto.Nutrients.Calories.Should().Be(233.4);
    }

    [Fact]
    public async Task Eat_Twice_ThrowsConflict()
    {
        // Arrange
        var meal = await Plan(PlannerTestData.Food(_me, (await AddOatsAsync()).Id));
        await Eat(meal.Id);

        // Act
        var act = () => Eat(meal.Id);

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage(PlannerErrors.AlreadyEaten);
        (await _ctx.Db.DiaryEntries.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Eat_DayAfterTomorrow_ThrowsValidation()
    {
        // Arrange
        var meal = await Plan(PlannerTestData.Food(_me, (await AddOatsAsync()).Id, date: PlannerTestData.Today.AddDays(2)));

        // Act
        var act = () => Eat(meal.Id);

        // Assert
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().ContainSingle(e => e.ErrorMessage == PlannerErrors.FutureMeal);
        (await _ctx.Db.DiaryEntries.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Eat_Tomorrow_Succeeds()
    {
        // Arrange
        var meal = await Plan(PlannerTestData.Food(_me, (await AddOatsAsync()).Id, date: PlannerTestData.Today.AddDays(1)));

        // Act
        var dto = await Eat(meal.Id);

        // Assert
        dto.Date.Should().Be(PlannerTestData.Today.AddDays(1));
    }

    [Fact]
    public async Task Eat_ArchivedFood_CreatesEntry()
    {
        // Arrange
        var oats = await AddOatsAsync();
        var meal = await Plan(PlannerTestData.Food(_me, oats.Id, grams: 100));
        oats.Archive(_ctx.Now);
        await _ctx.Db.SaveChangesAsync();

        // Act
        var dto = await Eat(meal.Id);

        // Assert
        dto.Food!.IsArchived.Should().BeTrue();
        dto.Nutrients.Calories.Should().Be(389);
    }

    [Fact]
    public async Task Eat_OtherUsersMeal_ThrowsNotFound()
    {
        // Arrange
        var meal = await Plan(PlannerTestData.Food(_me, (await AddOatsAsync()).Id));

        // Act
        var act = () => Eat(meal.Id, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task EditFoodAfterEating_KeepsEntrySnapshot()
    {
        // Arrange
        var oats = await AddOatsAsync();
        var meal = await Plan(PlannerTestData.Food(_me, oats.Id, grams: 100));
        await Eat(meal.Id);

        // Act
        oats.Calories = 400;
        await _ctx.Db.SaveChangesAsync();

        // Assert
        (await _ctx.Db.DiaryEntries.SingleAsync()).CalculatedCalories.Should().Be(389);
    }

    public void Dispose() => _ctx.Dispose();
}
