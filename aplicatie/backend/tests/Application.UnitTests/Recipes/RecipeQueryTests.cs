using FluentAssertions;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Recipes.Commands.ArchiveRecipe;
using NutriScan.Application.Recipes.Commands.CreateRecipe;
using NutriScan.Application.Recipes.Dtos;
using NutriScan.Application.Recipes.Queries.GetRecipe;
using NutriScan.Application.Recipes.Queries.SearchRecipes;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Application.UnitTests.Diary;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.UnitTests.Recipes;

public class RecipeQueryTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();
    private readonly Guid _me = Guid.NewGuid();
    private readonly FoodItem _oats;

    public RecipeQueryTests()
    {
        _oats = DiaryTestData.Oats(null);
        _ctx.Db.FoodItems.Add(_oats);
        _ctx.Db.SaveChanges();
    }

    private Task<RecipeDto> Create(string name = "Terci de ovăz", Guid? owner = null, FoodItem? food = null) =>
        new CreateRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(
            RecipeTestData.Create(owner ?? _me, ((food ?? _oats).Id, 200)) with { Name = name }, CancellationToken.None);

    private Task<RecipeDto> Get(Guid recipeId, Guid? userId = null) =>
        new GetRecipeQueryHandler(_ctx.Db).Handle(new GetRecipeQuery(userId ?? _me, recipeId), CancellationToken.None);

    private Task<PagedResult<RecipeSummaryDto>> Search(string? query = null, int page = 1, int pageSize = 20) =>
        new SearchRecipesQueryHandler(_ctx.Db).Handle(new SearchRecipesQuery(_me, query, page, pageSize), CancellationToken.None);

    [Fact]
    public async Task Get_OwnRecipe_ReturnsIngredientContributions()
    {
        // Arrange
        var created = await Create();

        // Act
        var dto = await Get(created.Id);

        // Assert
        var ingredient = dto.Ingredients.Should().ContainSingle().Subject;
        ingredient.Nutrients.Calories.Should().Be(778);
        ingredient.Food.IsArchived.Should().BeFalse();
        dto.PerServing.Calories.Should().Be(194.5);
    }

    [Fact]
    public async Task Get_ArchivedIngredient_IsFlagged()
    {
        // Arrange
        var own = DiaryTestData.Oats(_me);
        _ctx.Db.FoodItems.Add(own);
        await _ctx.Db.SaveChangesAsync();
        var created = await Create(food: own);
        own.Archive(_ctx.Now);
        await _ctx.Db.SaveChangesAsync();

        // Act
        var dto = await Get(created.Id);

        // Assert
        dto.Ingredients.Single().Food.IsArchived.Should().BeTrue();
        dto.Total.Calories.Should().Be(778);
    }

    [Fact]
    public async Task Get_OtherUsersRecipe_ThrowsNotFound()
    {
        // Arrange
        var created = await Create();

        // Act
        var act = () => Get(created.Id, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Get_ArchivedRecipe_ThrowsNotFound()
    {
        // Arrange
        var created = await Create();
        await new ArchiveRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(new ArchiveRecipeCommand(_me, created.Id), CancellationToken.None);

        // Act
        var act = () => Get(created.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Search_ReturnsOnlyOwnNotArchived_SortedByName()
    {
        // Arrange
        await Create("Supă");
        await Create("Ardei umpluți");
        await Create("Rețeta altcuiva", Guid.NewGuid());
        var archived = await Create("Arhivată");
        await new ArchiveRecipeCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(new ArchiveRecipeCommand(_me, archived.Id), CancellationToken.None);

        // Act
        var result = await Search();

        // Assert
        result.Items.Select(r => r.Name).Should().Equal("Ardei umpluți", "Supă");
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Search_IgnoresDiacriticsAndCase()
    {
        // Arrange
        await Create("Ciorbă de perișoare");
        await Create("Tocăniță");

        // Act
        var result = await Search("CIORBA");

        // Assert
        result.Items.Select(r => r.Name).Should().Equal("Ciorbă de perișoare");
    }

    [Fact]
    public async Task Search_PerServingAndIngredientCount()
    {
        // Arrange
        await Create();

        // Act
        var result = await Search();

        // Assert
        var summary = result.Items.Should().ContainSingle().Subject;
        summary.PerServing.Calories.Should().Be(194.5);
        summary.PerServing.Proteins.Should().Be(8.5);
        summary.IngredientCount.Should().Be(1);
        summary.ServingsCount.Should().Be(4);
        summary.PrepTimeMinutes.Should().Be(10);
    }

    [Fact]
    public async Task Search_Paginates()
    {
        // Arrange
        await Create("A");
        await Create("B");
        await Create("C");

        // Act
        var result = await Search(page: 2, pageSize: 2);

        // Assert
        result.Items.Select(r => r.Name).Should().Equal("C");
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public void Validate_Page0_IsInvalid() =>
        new SearchRecipesQueryValidator().Validate(new SearchRecipesQuery(_me, null, Page: 0)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_PageSize51_IsInvalid() =>
        new SearchRecipesQueryValidator().Validate(new SearchRecipesQuery(_me, null, PageSize: 51)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_Query201Chars_IsInvalid() =>
        new SearchRecipesQueryValidator().Validate(new SearchRecipesQuery(_me, new string('a', 201))).IsValid.Should().BeFalse();

    public void Dispose() => _ctx.Dispose();
}
