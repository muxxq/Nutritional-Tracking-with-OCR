using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Application.Planner.Commands.CreatePlannedMeal;
using NutriScan.Application.Planner.Commands.DeletePlannedMeal;
using NutriScan.Application.Planner.Commands.UpdatePlannedMeal;
using NutriScan.Application.Planner.Dtos;
using NutriScan.Application.Recipes.Commands.ArchiveRecipe;
using NutriScan.Application.Recipes.Commands.CreateRecipe;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Application.UnitTests.Diary;
using NutriScan.Application.UnitTests.Recipes;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Planner;

public class PlannedMealCommandTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();
    private readonly Guid _me = Guid.NewGuid();

    private async Task<FoodItem> AddFoodAsync(FoodItem food)
    {
        _ctx.Db.FoodItems.Add(food);
        await _ctx.Db.SaveChangesAsync();
        return food;
    }

    private async Task<Guid> CreateSrsRecipeAsync(FoodItem oats) =>
        (await new CreateRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(RecipeTestData.Create(_me, (oats.Id, 200)), CancellationToken.None)).Id;

    private Task<PlannedMealDto> Create(CreatePlannedMealCommand command) =>
        new CreatePlannedMealCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(command, CancellationToken.None);

    private Task<PlannedMealDto> Update(UpdatePlannedMealCommand command) =>
        new UpdatePlannedMealCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(command, CancellationToken.None);

    private Task Delete(Guid userId, Guid mealId) =>
        new DeletePlannedMealCommandHandler(_ctx.Db).Handle(new DeletePlannedMealCommand(userId, mealId), CancellationToken.None);

    [Fact]
    public async Task Create_Recipe_ComputesFromCurrentRecipe()
    {
        // Arrange
        var recipeId = await CreateSrsRecipeAsync(await AddFoodAsync(DiaryTestData.Oats(_me)));

        // Act
        var dto = await Create(PlannerTestData.Recipe(_me, recipeId, servings: 2));

        // Assert
        dto.Kind.Should().Be(DiaryEntryKind.Recipe);
        dto.Nutrients.Calories.Should().Be(389);
        dto.QuantityGrams.Should().BeNull();
        dto.Food.Should().BeNull();
        dto.Recipe!.Name.Should().Be("Terci de ovăz");
        dto.DiaryEntryId.Should().BeNull();
        var stored = await _ctx.Db.PlannedMeals.SingleAsync();
        stored.UserId.Should().Be(_me);
        stored.CreatedAtUtc.Should().Be(_ctx.Now);
        stored.PlanDate.Should().Be(PlannerTestData.Today);
    }

    [Fact]
    public async Task Create_FoodInServings_ConvertsToGrams()
    {
        // Arrange
        var oats = await AddFoodAsync(DiaryTestData.Oats(_me));

        // Act
        var dto = await Create(PlannerTestData.Food(_me, oats.Id, grams: null, servings: 1.5));

        // Assert
        dto.Kind.Should().Be(DiaryEntryKind.Food);
        dto.QuantityGrams.Should().Be(60);
        dto.Servings.Should().Be(1.5);
        dto.Nutrients.Calories.Should().Be(233.4);
    }

    [Fact]
    public async Task Create_FoodServingsWithoutServingSize_ThrowsValidation()
    {
        // Arrange
        var chicken = await AddFoodAsync(DiaryTestData.Chicken());

        // Act
        var act = () => Create(PlannerTestData.Food(_me, chicken.Id, grams: null, servings: 1));

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Create_OtherUsersFood_ThrowsNotFound()
    {
        // Arrange
        var foreign = await AddFoodAsync(DiaryTestData.Oats(Guid.NewGuid()));

        // Act
        var act = () => Create(PlannerTestData.Food(_me, foreign.Id));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_ArchivedRecipe_ThrowsNotFound()
    {
        // Arrange
        var recipeId = await CreateSrsRecipeAsync(await AddFoodAsync(DiaryTestData.Oats(_me)));
        await new ArchiveRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(new ArchiveRecipeCommand(_me, recipeId), CancellationToken.None);

        // Act
        var act = () => Create(PlannerTestData.Recipe(_me, recipeId));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        (await _ctx.Db.PlannedMeals.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Update_ChangesDateMealAndServings()
    {
        // Arrange
        var recipeId = await CreateSrsRecipeAsync(await AddFoodAsync(DiaryTestData.Oats(_me)));
        var created = await Create(PlannerTestData.Recipe(_me, recipeId, servings: 2));
        _ctx.Now = _ctx.Now.AddHours(1);

        // Act
        var dto = await Update(PlannerTestData.Update(_me, created.Id, servings: 1, meal: MealType.Dinner));

        // Assert
        dto.Date.Should().Be(PlannerTestData.Today.AddDays(1));
        dto.MealType.Should().Be(MealType.Dinner);
        dto.Nutrients.Calories.Should().Be(194.5);
        (await _ctx.Db.PlannedMeals.SingleAsync()).UpdatedAtUtc.Should().Be(_ctx.Now);
    }

    [Fact]
    public async Task Update_RecipeMealWithGrams_ThrowsValidation()
    {
        // Arrange
        var recipeId = await CreateSrsRecipeAsync(await AddFoodAsync(DiaryTestData.Oats(_me)));
        var created = await Create(PlannerTestData.Recipe(_me, recipeId));

        // Act
        var act = () => Update(PlannerTestData.Update(_me, created.Id, grams: 300, servings: null));

        // Assert
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Quantity");
        (await _ctx.CreateContext().PlannedMeals.SingleAsync()).QuantityGrams.Should().BeNull();
    }

    [Fact]
    public async Task Update_ArchivedFoodMeal_Succeeds()
    {
        // Arrange
        var oats = await AddFoodAsync(DiaryTestData.Oats(_me));
        var created = await Create(PlannerTestData.Food(_me, oats.Id, grams: 100));
        oats.Archive(_ctx.Now);
        await _ctx.Db.SaveChangesAsync();

        // Act
        var dto = await Update(PlannerTestData.Update(_me, created.Id, grams: 200, servings: null));

        // Assert
        dto.Nutrients.Calories.Should().Be(778);
        dto.Food!.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task Update_OtherUsersMeal_ThrowsNotFound()
    {
        // Arrange
        var oats = await AddFoodAsync(DiaryTestData.Oats(null));
        var created = await Create(PlannerTestData.Food(_me, oats.Id));

        // Act
        var act = () => Update(PlannerTestData.Update(Guid.NewGuid(), created.Id, grams: 50, servings: null));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_RemovesMeal()
    {
        // Arrange
        var oats = await AddFoodAsync(DiaryTestData.Oats(null));
        var created = await Create(PlannerTestData.Food(_me, oats.Id));

        // Act
        await Delete(_me, created.Id);

        // Assert
        (await _ctx.Db.PlannedMeals.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_OtherUsersMeal_ThrowsNotFound()
    {
        // Arrange
        var oats = await AddFoodAsync(DiaryTestData.Oats(null));
        var created = await Create(PlannerTestData.Food(_me, oats.Id));

        // Act
        var act = () => Delete(Guid.NewGuid(), created.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        (await _ctx.Db.PlannedMeals.AnyAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task Delete_EatenMeal_KeepsDiaryEntry()
    {
        // Arrange
        var oats = await AddFoodAsync(DiaryTestData.Oats(null));
        var created = await Create(PlannerTestData.Food(_me, oats.Id));
        var entry = new DiaryEntry
        {
            UserId = _me, FoodItemId = oats.Id, ConsumedDate = PlannerTestData.Today, MealType = MealType.Lunch, QuantityGrams = 100
        };
        entry.RecalculateNutrients(oats);
        _ctx.Db.DiaryEntries.Add(entry);
        (await _ctx.Db.PlannedMeals.SingleAsync()).DiaryEntryId = entry.Id;
        await _ctx.Db.SaveChangesAsync();

        // Act
        await Delete(_me, created.Id);

        // Assert
        (await _ctx.Db.PlannedMeals.AnyAsync()).Should().BeFalse();
        (await _ctx.Db.DiaryEntries.SingleAsync()).Id.Should().Be(entry.Id);
    }

    public void Dispose() => _ctx.Dispose();
}
