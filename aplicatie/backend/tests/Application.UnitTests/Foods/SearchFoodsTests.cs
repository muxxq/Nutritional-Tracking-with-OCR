using FluentAssertions;
using NutriScan.Application.Foods;
using NutriScan.Application.Foods.Queries.SearchFoods;
using NutriScan.Application.UnitTests.Auth;

namespace NutriScan.Application.UnitTests.Foods;

public class SearchFoodsTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();
    private readonly Guid _me = Guid.NewGuid();

    public SearchFoodsTests()
    {
        var archived = FoodTestData.Food("Pui vechi", _me);
        archived.Archive(_ctx.Now);
        _ctx.Db.FoodItems.AddRange(
            FoodTestData.Food("Piept de pui fără piele", verified: true),
            FoodTestData.Food("Pui întreg", verified: true),
            FoodTestData.Food("Pui la grătar", _me),
            FoodTestData.Food("Pui secret", Guid.NewGuid()),
            archived);
        _ctx.Db.SaveChanges();
    }

    private Task<NutriScan.Application.Common.Models.PagedResult<NutriScan.Application.Foods.Dtos.FoodSummaryDto>> Search(
        string? query, FoodScope scope = FoodScope.All, int page = 1, int pageSize = 20) =>
        new SearchFoodsQueryHandler(_ctx.Db).Handle(new SearchFoodsQuery(_me, query, scope, page, pageSize), CancellationToken.None);

    [Fact]
    public async Task Search_WithoutDiacritics_FindsFood()
    {
        // Act
        var result = await Search("fara piele");

        // Assert
        result.Items.Select(f => f.Name).Should().Equal("Piept de pui fără piele");
    }

    [Fact]
    public async Task Search_Pui_OrdersPrefixThenVerifiedThenName_AndHidesOthers()
    {
        // Act
        var result = await Search("pui");

        // Assert
        result.Items.Select(f => f.Name).Should().Equal("Pui întreg", "Pui la grătar", "Piept de pui fără piele");
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Search_MultipleWords_RequiresAll()
    {
        // Act
        var result = await Search("pui gratar");

        // Assert
        result.Items.Select(f => f.Name).Should().Equal("Pui la grătar");
    }

    [Fact]
    public async Task Search_ScopeMine_ReturnsOnlyOwnNonArchived()
    {
        // Act
        var result = await Search(null, FoodScope.Mine);

        // Assert
        result.Items.Should().ContainSingle(f => f.Name == "Pui la grătar" && f.IsOwn);
    }

    [Fact]
    public async Task Search_Paging_ReturnsRequestedPage()
    {
        // Act
        var result = await Search("pui", page: 2, pageSize: 1);

        // Assert
        result.Items.Select(f => f.Name).Should().Equal("Pui la grătar");
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(2);
    }

    [Fact]
    public async Task Search_IsOwn_MarksOnlyMyFoods()
    {
        // Act
        var result = await Search("pui");

        // Assert
        result.Items.Where(f => f.IsOwn).Select(f => f.Name).Should().Equal("Pui la grătar");
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    [InlineData(10_001, 20)]
    [InlineData(int.MaxValue, 50)]
    public void Validator_RejectsInvalidPaging(int page, int pageSize)
    {
        // Act
        var result = new SearchFoodsQueryValidator().Validate(new SearchFoodsQuery(_me, null, FoodScope.All, page, pageSize));

        // Assert
        result.IsValid.Should().BeFalse();
    }

    public void Dispose() => _ctx.Dispose();
}
