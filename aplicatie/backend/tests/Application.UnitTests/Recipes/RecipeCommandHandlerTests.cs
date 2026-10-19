using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Recipes;
using NutriScan.Application.Recipes.Commands.ArchiveRecipe;
using NutriScan.Application.Recipes.Commands.CreateRecipe;
using NutriScan.Application.Recipes.Commands.UpdateRecipe;
using NutriScan.Application.Recipes.Dtos;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Application.UnitTests.Diary;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.UnitTests.Recipes;

public class RecipeCommandHandlerTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();
    private readonly Guid _me = Guid.NewGuid();

    private async Task<FoodItem> AddAsync(FoodItem food)
    {
        _ctx.Db.FoodItems.Add(food);
        await _ctx.Db.SaveChangesAsync();
        return food;
    }

    private Task<RecipeDto> Create(CreateRecipeCommand command) =>
        new CreateRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(command, CancellationToken.None);

    private Task<RecipeDto> Update(UpdateRecipeCommand command) =>
        new UpdateRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(command, CancellationToken.None);

    private Task Archive(Guid userId, Guid recipeId) =>
        new ArchiveRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(new ArchiveRecipeCommand(userId, recipeId), CancellationToken.None);

    private async Task<RecipeDto> CreateSrsRecipeAsync(FoodItem oats) =>
        await Create(RecipeTestData.Create(_me, (oats.Id, 200)));

    [Fact]
    public async Task Create_SrsExample_ReturnsTotalsAndPerServing()
    {
        // Arrange
        var oats = await AddAsync(DiaryTestData.Oats(_me));

        // Act
        var dto = await CreateSrsRecipeAsync(oats);

        // Assert
        dto.Total.Calories.Should().Be(778);
        dto.PerServing.Calories.Should().Be(194.5);
        dto.Name.Should().Be("Terci de ovăz");
        var stored = await _ctx.Db.Recipes.Include(r => r.Ingredients).SingleAsync();
        stored.UserId.Should().Be(_me);
        stored.CreatedAtUtc.Should().Be(_ctx.Now);
        stored.Name.Should().Be("Terci de ovăz");
        stored.Ingredients.Should().ContainSingle(i => i.FoodItemId == oats.Id && i.QuantityGrams == 200);
    }

    [Fact]
    public async Task Create_GlobalFood_Succeeds()
    {
        // Arrange
        var chicken = await AddAsync(DiaryTestData.Chicken());

        // Act
        var dto = await Create(RecipeTestData.Create(_me, (chicken.Id, 150)));

        // Assert
        dto.Total.Calories.Should().Be(180);
    }

    [Fact]
    public async Task Create_OtherUsersFood_ThrowsNotFound()
    {
        // Arrange
        var foreign = await AddAsync(DiaryTestData.Oats(Guid.NewGuid()));

        // Act
        var act = () => Create(RecipeTestData.Create(_me, (foreign.Id, 200)));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        (await _ctx.Db.Recipes.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Create_ArchivedFood_ThrowsNotFound()
    {
        // Arrange
        var oats = DiaryTestData.Oats(_me);
        oats.Archive(_ctx.Now);
        await AddAsync(oats);

        // Act
        var act = () => Create(RecipeTestData.Create(_me, (oats.Id, 200)));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Update_ReplacesFieldsAndIngredients()
    {
        // Arrange
        var oats = await AddAsync(DiaryTestData.Oats(_me));
        var chicken = await AddAsync(DiaryTestData.Chicken());
        var created = await CreateSrsRecipeAsync(oats);
        _ctx.Now = _ctx.Now.AddHours(1);

        // Act
        var dto = await Update(RecipeTestData.Update(_me, created.Id,
            RecipeTestData.Create(_me, (oats.Id, 100), (chicken.Id, 150)) with { Name = "Bol", ServingsCount = 2 }));

        // Assert
        dto.Name.Should().Be("Bol");
        dto.Ingredients.Should().HaveCount(2);
        dto.Total.Calories.Should().Be(569);
        var stored = await _ctx.Db.Recipes.Include(r => r.Ingredients).SingleAsync();
        stored.Ingredients.Should().HaveCount(2);
        stored.Ingredients.Single(i => i.FoodItemId == oats.Id).QuantityGrams.Should().Be(100);
        stored.ServingsCount.Should().Be(2);
        stored.UpdatedAtUtc.Should().Be(_ctx.Now);
    }

    [Fact]
    public async Task Update_KeepsArchivedIngredient_Succeeds()
    {
        // Arrange
        var oats = await AddAsync(DiaryTestData.Oats(_me));
        var created = await CreateSrsRecipeAsync(oats);
        oats.Archive(_ctx.Now);
        await _ctx.Db.SaveChangesAsync();

        // Act
        var dto = await Update(RecipeTestData.Update(_me, created.Id, RecipeTestData.Create(_me, (oats.Id, 300))));

        // Assert
        dto.Ingredients.Should().ContainSingle(i => i.Food.Id == oats.Id && i.Food.IsArchived && i.QuantityGrams == 300);
    }

    [Fact]
    public async Task Update_AddsArchivedFood_ThrowsNotFound()
    {
        // Arrange
        var oats = await AddAsync(DiaryTestData.Oats(_me));
        var archived = DiaryTestData.Chicken(_me);
        archived.Archive(_ctx.Now);
        await AddAsync(archived);
        var created = await CreateSrsRecipeAsync(oats);

        // Act
        var act = () => Update(RecipeTestData.Update(_me, created.Id,
            RecipeTestData.Create(_me, (oats.Id, 200), (archived.Id, 100))));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Update_OtherUsersRecipe_ThrowsNotFound()
    {
        // Arrange
        var oats = await AddAsync(DiaryTestData.Oats(null));
        var created = await CreateSrsRecipeAsync(oats);
        var stranger = Guid.NewGuid();

        // Act
        var act = () => Update(RecipeTestData.Update(stranger, created.Id, RecipeTestData.Create(stranger, (oats.Id, 100))));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage(RecipeErrors.NotFound);
    }

    [Fact]
    public async Task Update_ArchivedRecipe_ThrowsNotFound()
    {
        // Arrange
        var oats = await AddAsync(DiaryTestData.Oats(_me));
        var created = await CreateSrsRecipeAsync(oats);
        await Archive(_me, created.Id);

        // Act
        var act = () => Update(RecipeTestData.Update(_me, created.Id, RecipeTestData.Create(_me, (oats.Id, 100))));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Archive_SetsArchivedAt()
    {
        // Arrange
        var oats = await AddAsync(DiaryTestData.Oats(_me));
        var created = await CreateSrsRecipeAsync(oats);

        // Act
        await Archive(_me, created.Id);

        // Assert
        (await _ctx.Db.Recipes.SingleAsync()).ArchivedAtUtc.Should().Be(_ctx.Now);
    }

    [Fact]
    public async Task Archive_Twice_ThrowsNotFound()
    {
        // Arrange
        var oats = await AddAsync(DiaryTestData.Oats(_me));
        var created = await CreateSrsRecipeAsync(oats);
        await Archive(_me, created.Id);

        // Act
        var act = () => Archive(_me, created.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Archive_OtherUsersRecipe_ThrowsNotFound()
    {
        // Arrange
        var oats = await AddAsync(DiaryTestData.Oats(_me));
        var created = await CreateSrsRecipeAsync(oats);

        // Act
        var act = () => Archive(Guid.NewGuid(), created.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        (await _ctx.Db.Recipes.SingleAsync()).ArchivedAtUtc.Should().BeNull();
    }

    public void Dispose() => _ctx.Dispose();
}
