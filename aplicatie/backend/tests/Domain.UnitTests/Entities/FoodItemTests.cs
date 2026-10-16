using FluentAssertions;
using NutriScan.Domain.Entities;
using Xunit;

namespace NutriScan.Domain.UnitTests.Entities;

public class FoodItemTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SearchText_FollowsNameAndBrand()
    {
        // Arrange
        var food = new FoodItem { Name = "Fără zahăr", Brand = "Ţară" };

        // Act
        var withBrand = food.SearchText;
        food.Brand = null;

        // Assert
        withBrand.Should().Be("fara zahar tara");
        food.SearchText.Should().Be("fara zahar");
    }

    [Fact]
    public void IsVisibleTo_GlobalOwnOthersAndArchived()
    {
        // Arrange
        var me = Guid.NewGuid();
        var global = new FoodItem { Name = "Global" };
        var mine = new FoodItem { Name = "Al meu", CreatedByUserId = me };
        var others = new FoodItem { Name = "Al altcuiva", CreatedByUserId = Guid.NewGuid() };
        var archived = new FoodItem { Name = "Arhivat", CreatedByUserId = me };
        archived.Archive(Now);

        // Assert
        global.IsGlobal.Should().BeTrue();
        global.IsVisibleTo(me).Should().BeTrue();
        mine.IsVisibleTo(me).Should().BeTrue();
        others.IsVisibleTo(me).Should().BeFalse();
        archived.IsVisibleTo(me).Should().BeFalse();
    }

    [Fact]
    public void Archive_SetsDate()
    {
        // Arrange
        var food = new FoodItem { Name = "Pui", CreatedByUserId = Guid.NewGuid() };

        // Act
        food.Archive(Now);

        // Assert
        food.ArchivedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void EnergyWarning_UsesDeclaredValues()
    {
        // Arrange
        var food = new FoodItem { Name = "X", Calories = 250, Proteins = 10, Carbohydrates = 30, Fats = 5 };

        // Act
        var warning = food.EnergyWarning();

        // Assert
        warning.Should().Contain("18%");
    }
}
