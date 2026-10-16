using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Diary;
using NutriScan.Application.Diary.Commands.AddDiaryEntry;
using NutriScan.Application.Diary.Commands.UpdateDiaryEntry;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Application.Diary.Queries.GetDiaryDay;
using NutriScan.Application.Diary.Queries.GetRecentFoods;
using NutriScan.Application.Recipes.Commands.ArchiveRecipe;
using NutriScan.Application.Recipes.Commands.CreateRecipe;
using NutriScan.Application.Recipes.Commands.UpdateRecipe;
using NutriScan.Application.Recipes.Dtos;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Application.UnitTests.Recipes;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Diary;

public class DiaryRecipeEntryTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();
    private User _user = null!;
    private FoodItem _oats = null!;

    private Guid Me => _user.Id;

    private async Task<RecipeDto> ArrangeSrsRecipeAsync()
    {
        _user = await _ctx.SeedUserAsync();
        _oats = DiaryTestData.Oats(Me);
        _ctx.Db.FoodItems.Add(_oats);
        await _ctx.Db.SaveChangesAsync();
        return await new CreateRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(RecipeTestData.Create(Me, (_oats.Id, 200)), CancellationToken.None);
    }

    private async Task<DiaryEntryDto> Add(AddDiaryEntryCommand command)
    {
        var dto = await new AddDiaryEntryCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(command, CancellationToken.None);
        _ctx.Now = _ctx.Now.AddMinutes(1);
        return dto;
    }

    private Task<DiaryEntryDto> UpdateEntry(Guid entryId, double? servings, double? grams = null) =>
        new UpdateDiaryEntryCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(
            new UpdateDiaryEntryCommand(Me, entryId, DiaryTestData.Today, MealType.Lunch, grams, servings), CancellationToken.None);

    private AddDiaryEntryCommandValidator Validator() => new(_ctx.Clock.Object);

    [Fact]
    public async Task Add_Recipe_StoresServingsAndSnapshot()
    {
        // Arrange
        var recipe = await ArrangeSrsRecipeAsync();

        // Act
        var dto = await Add(DiaryTestData.AddRecipe(Me, recipe.Id, servings: 1.5));

        // Assert
        dto.Kind.Should().Be(DiaryEntryKind.Recipe);
        dto.QuantityGrams.Should().BeNull();
        dto.Servings.Should().Be(1.5);
        dto.Food.Should().BeNull();
        dto.Recipe.Should().Be(new DiaryRecipeDto(recipe.Id, "Terci de ovăz", false));
        dto.Nutrients.Calories.Should().Be(291.8);
        var stored = await _ctx.Db.DiaryEntries.SingleAsync();
        stored.CalculatedCalories.Should().Be(291.75);
        stored.RecipeId.Should().Be(recipe.Id);
        stored.FoodItemId.Should().BeNull();
    }

    [Fact]
    public async Task Add_OtherUsersRecipe_ThrowsNotFound()
    {
        // Arrange
        var recipe = await ArrangeSrsRecipeAsync();

        // Act
        var act = () => Add(DiaryTestData.AddRecipe(Guid.NewGuid(), recipe.Id));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        (await _ctx.Db.DiaryEntries.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Add_ArchivedRecipe_ThrowsNotFound()
    {
        // Arrange
        var recipe = await ArrangeSrsRecipeAsync();
        await new ArchiveRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(new ArchiveRecipeCommand(Me, recipe.Id), CancellationToken.None);

        // Act
        var act = () => Add(DiaryTestData.AddRecipe(Me, recipe.Id));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Validate_FoodAndRecipe_IsInvalid()
    {
        // Act
        var result = Validator().Validate(DiaryTestData.Add(Guid.NewGuid(), Guid.NewGuid()) with { RecipeId = Guid.NewGuid() });

        // Assert
        result.Errors.Should().Contain(e => e.ErrorMessage == DiaryErrors.ExactlyOneItem);
    }

    [Fact]
    public void Validate_NeitherFoodNorRecipe_IsInvalid()
    {
        // Act
        var result = Validator().Validate(DiaryTestData.Add(Guid.NewGuid(), Guid.NewGuid()) with { FoodId = null });

        // Assert
        result.Errors.Should().Contain(e => e.ErrorMessage == DiaryErrors.ExactlyOneItem);
    }

    [Fact]
    public void Validate_RecipeWithGrams_IsInvalid()
    {
        // Act
        var result = Validator().Validate(DiaryTestData.AddRecipe(Guid.NewGuid(), Guid.NewGuid(), servings: null, grams: 300));

        // Assert
        result.Errors.Should().Contain(e => e.ErrorMessage == DiaryErrors.RecipeServingsOnly);
    }

    [Fact]
    public void Validate_RecipeWithoutServings_IsInvalid()
    {
        // Act
        var result = Validator().Validate(DiaryTestData.AddRecipe(Guid.NewGuid(), Guid.NewGuid(), servings: null));

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Update_RecipeEntryServings_RecalculatesFromCurrentRecipe()
    {
        // Arrange
        var recipe = await ArrangeSrsRecipeAsync();
        var entry = await Add(DiaryTestData.AddRecipe(Me, recipe.Id));
        await new UpdateRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(
            RecipeTestData.Update(Me, recipe.Id, RecipeTestData.Create(Me, (_oats.Id, 400))), CancellationToken.None);

        // Act
        var dto = await UpdateEntry(entry.Id, servings: 2);

        // Assert: 400 g oats = 1556 kcal, 4 servings → 389 per serving, × 2
        dto.Nutrients.Calories.Should().Be(778);
        dto.MealType.Should().Be(MealType.Lunch);
        dto.Kind.Should().Be(DiaryEntryKind.Recipe);
    }

    [Fact]
    public async Task Update_RecipeEntryOfArchivedRecipe_Succeeds()
    {
        // Arrange
        var recipe = await ArrangeSrsRecipeAsync();
        var entry = await Add(DiaryTestData.AddRecipe(Me, recipe.Id));
        await new ArchiveRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(new ArchiveRecipeCommand(Me, recipe.Id), CancellationToken.None);

        // Act
        var dto = await UpdateEntry(entry.Id, servings: 2);

        // Assert
        dto.Nutrients.Calories.Should().Be(389);
        dto.Recipe!.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task Update_RecipeEntryWithGrams_ThrowsValidation()
    {
        // Arrange
        var recipe = await ArrangeSrsRecipeAsync();
        var entry = await Add(DiaryTestData.AddRecipe(Me, recipe.Id));

        // Act
        var act = () => UpdateEntry(entry.Id, servings: null, grams: 300);

        // Assert
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Quantity" && e.ErrorMessage == DiaryErrors.RecipeServingsOnly);
        (await _ctx.CreateContext().DiaryEntries.SingleAsync()).QuantityGrams.Should().BeNull();
    }

    [Fact]
    public async Task EditingFoodAfterLoggingRecipe_KeepsSnapshot()
    {
        // Arrange
        var recipe = await ArrangeSrsRecipeAsync();
        await Add(DiaryTestData.AddRecipe(Me, recipe.Id, servings: 1));

        // Act
        _oats.Calories = 400;
        await _ctx.Db.SaveChangesAsync();

        // Assert
        (await _ctx.Db.DiaryEntries.SingleAsync()).CalculatedCalories.Should().Be(194.5);
    }

    [Fact]
    public async Task GetDay_MixedEntries_SumsBothKinds()
    {
        // Arrange
        var recipe = await ArrangeSrsRecipeAsync();
        var food = await Add(DiaryTestData.Add(Me, _oats.Id, grams: 100));
        var fromRecipe = await Add(DiaryTestData.AddRecipe(Me, recipe.Id, servings: 1));

        // Act
        var day = await new GetDiaryDayQueryHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(new GetDiaryDayQuery(Me, DiaryTestData.Today), CancellationToken.None);

        // Assert
        var breakfast = day.Meals.Single(m => m.MealType == MealType.Breakfast);
        breakfast.Entries.Select(e => e.Id).Should().Equal(food.Id, fromRecipe.Id);
        breakfast.Entries.Select(e => e.Kind).Should().Equal(DiaryEntryKind.Food, DiaryEntryKind.Recipe);
        breakfast.Totals.Calories.Should().Be(583.5);
        day.Totals.Calories.Should().Be(583.5);
    }

    [Fact]
    public async Task GetRecentFoods_IgnoresRecipeEntries()
    {
        // Arrange
        var recipe = await ArrangeSrsRecipeAsync();
        await Add(DiaryTestData.Add(Me, _oats.Id, grams: 100));
        await Add(DiaryTestData.AddRecipe(Me, recipe.Id));

        // Act
        var recent = await new GetRecentFoodsQueryHandler(_ctx.Db).Handle(new GetRecentFoodsQuery(Me), CancellationToken.None);

        // Assert
        recent.Should().ContainSingle().Which.Food.Id.Should().Be(_oats.Id);
    }

    public void Dispose() => _ctx.Dispose();
}
