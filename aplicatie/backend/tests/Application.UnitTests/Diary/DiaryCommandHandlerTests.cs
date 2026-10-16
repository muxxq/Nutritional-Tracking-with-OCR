using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Diary.Commands.AddDiaryEntry;
using NutriScan.Application.Diary.Commands.DeleteDiaryEntry;
using NutriScan.Application.Diary.Commands.UpdateDiaryEntry;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Application.Diary.Queries.GetDiaryEntry;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Diary;

public class DiaryCommandHandlerTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();
    private readonly Guid _me = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();

    private async Task<FoodItem> AddFoodAsync(FoodItem food)
    {
        _ctx.Db.FoodItems.Add(food);
        await _ctx.Db.SaveChangesAsync();
        return food;
    }

    private Task<DiaryEntryDto> Add(AddDiaryEntryCommand command) =>
        new AddDiaryEntryCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(command, CancellationToken.None);

    private Task<DiaryEntryDto> Update(Guid userId, Guid entryId, double? grams = 100, double? servings = null,
        MealType meal = MealType.Breakfast, DateOnly? date = null) =>
        new UpdateDiaryEntryCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(
            new UpdateDiaryEntryCommand(userId, entryId, date ?? DiaryTestData.Today, meal, grams, servings), CancellationToken.None);

    private Task Delete(Guid userId, Guid entryId) =>
        new DeleteDiaryEntryCommandHandler(_ctx.Db).Handle(new DeleteDiaryEntryCommand(userId, entryId), CancellationToken.None);

    private Task<DiaryEntryDto> Get(Guid userId, Guid entryId) =>
        new GetDiaryEntryQueryHandler(_ctx.Db).Handle(new GetDiaryEntryQuery(userId, entryId), CancellationToken.None);

    private async Task<DiaryEntryDto> AddOatsAsync()
    {
        var oats = await AddFoodAsync(DiaryTestData.Oats(_me));
        return await Add(DiaryTestData.Add(_me, oats.Id));
    }

    [Fact]
    public async Task Add_Grams_StoresSnapshot()
    {
        // Arrange
        var oats = await AddFoodAsync(DiaryTestData.Oats(_me));

        // Act
        var dto = await Add(DiaryTestData.Add(_me, oats.Id, grams: 100));

        // Assert
        dto.QuantityGrams.Should().Be(100);
        dto.Servings.Should().BeNull();
        dto.Nutrients.Should().Be(new NutrientsDto(389, 16.9, 66.3, 6.9));
        var stored = await _ctx.Db.DiaryEntries.SingleAsync();
        stored.UserId.Should().Be(_me);
        stored.CalculatedCalories.Should().Be(389);
        stored.CreatedAtUtc.Should().Be(_ctx.Now);
    }

    [Fact]
    public async Task Add_Servings_StoresGramsAndServings()
    {
        // Arrange
        var oats = await AddFoodAsync(DiaryTestData.Oats(_me));

        // Act
        var dto = await Add(DiaryTestData.Add(_me, oats.Id, grams: null, servings: 1.5));

        // Assert
        dto.QuantityGrams.Should().Be(60);
        dto.Servings.Should().Be(1.5);
        dto.Nutrients.Should().Be(new NutrientsDto(233.4, 10.1, 39.8, 4.1));
        dto.Food.Should().Be(new DiaryFoodDto(oats.Id, "Fulgi de ovăz", "Generic", false));
    }

    [Fact]
    public async Task Add_ServingsForFoodWithoutServingSize_ThrowsValidation()
    {
        // Arrange
        var oats = await AddFoodAsync(DiaryTestData.Oats(_me, servingSizeGrams: null));

        // Act
        var act = () => Add(DiaryTestData.Add(_me, oats.Id, grams: null, servings: 2));

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        (await _ctx.Db.DiaryEntries.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Add_ServingsAboveLimitAfterConversion_ThrowsValidation()
    {
        // Arrange
        var bigServing = await AddFoodAsync(DiaryTestData.Oats(_me, servingSizeGrams: 200));

        // Act
        var act = () => Add(DiaryTestData.Add(_me, bigServing.Id, grams: null, servings: 50));

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Add_GlobalFood_Succeeds()
    {
        // Arrange
        var chicken = await AddFoodAsync(DiaryTestData.Chicken());

        // Act
        var dto = await Add(DiaryTestData.Add(_me, chicken.Id, grams: 200));

        // Assert
        dto.Nutrients.Calories.Should().Be(240);
    }

    [Fact]
    public async Task Add_OtherUsersFood_ThrowsNotFound()
    {
        // Arrange
        var theirs = await AddFoodAsync(DiaryTestData.Oats(_other));

        // Act
        var act = () => Add(DiaryTestData.Add(_me, theirs.Id));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Add_ArchivedFood_ThrowsNotFound()
    {
        // Arrange
        var oats = DiaryTestData.Oats(_me);
        oats.Archive(_ctx.Now);
        await AddFoodAsync(oats);

        // Act
        var act = () => Add(DiaryTestData.Add(_me, oats.Id));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Update_FoodChangedAfterAdd_RecalculatesFromCurrentFood()
    {
        // Arrange
        var entry = await AddOatsAsync();
        var oats = await _ctx.Db.FoodItems.SingleAsync();
        oats.Calories = 400;
        await _ctx.Db.SaveChangesAsync();

        // Act
        var before = await Get(_me, entry.Id);
        var after = await Update(_me, entry.Id, grams: 100);

        // Assert: the snapshot survives the food edit until the entry itself is edited
        before.Nutrients.Calories.Should().Be(389);
        after.Nutrients.Calories.Should().Be(400);
    }

    [Fact]
    public async Task Update_ChangesQuantityMealAndDate()
    {
        // Arrange
        var entry = await AddOatsAsync();
        _ctx.Now = _ctx.Now.AddHours(1);

        // Act
        var dto = await Update(_me, entry.Id, grams: null, servings: 2, meal: MealType.Snack, date: DiaryTestData.Today.AddDays(-1));

        // Assert
        dto.QuantityGrams.Should().Be(80);
        dto.Servings.Should().Be(2);
        dto.MealType.Should().Be(MealType.Snack);
        dto.Date.Should().Be(DiaryTestData.Today.AddDays(-1));
        (await _ctx.Db.DiaryEntries.SingleAsync()).UpdatedAtUtc.Should().Be(_ctx.Now);
    }

    [Fact]
    public async Task Update_EntryWithArchivedFood_Succeeds()
    {
        // Arrange
        var entry = await AddOatsAsync();
        (await _ctx.Db.FoodItems.SingleAsync()).Archive(_ctx.Now);
        await _ctx.Db.SaveChangesAsync();

        // Act
        var dto = await Update(_me, entry.Id, grams: 50);

        // Assert
        dto.Food!.IsArchived.Should().BeTrue();
        dto.Nutrients.Calories.Should().Be(194.5);
    }

    [Fact]
    public async Task Update_OtherUsersEntry_ThrowsNotFound()
    {
        // Arrange
        var entry = await AddOatsAsync();

        // Act
        var act = () => Update(_other, entry.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_OtherUsersEntry_ThrowsNotFound()
    {
        // Arrange
        var entry = await AddOatsAsync();

        // Act
        var act = () => Delete(_other, entry.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        (await _ctx.Db.DiaryEntries.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Get_OtherUsersEntry_ThrowsNotFound()
    {
        // Arrange
        var entry = await AddOatsAsync();

        // Act
        var act = () => Get(_other, entry.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_OwnEntry_RemovesIt()
    {
        // Arrange
        var entry = await AddOatsAsync();

        // Act
        await Delete(_me, entry.Id);

        // Assert
        (await _ctx.Db.DiaryEntries.CountAsync()).Should().Be(0);
        var act = () => Get(_me, entry.Id);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _ctx.Dispose();
}
