using FluentAssertions;
using NutriScan.Domain.Entities;
using Xunit;

namespace NutriScan.Domain.UnitTests.Entities;

public class RecipeTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    [Fact]
    public void Name_Set_RefreshesSearchText()
    {
        // Act
        var recipe = new Recipe { Name = "Ciorbă de PERIȘOARE" };

        // Assert
        recipe.SearchText.Should().Be("ciorba de perisoare");
    }

    [Fact]
    public void IsVisibleTo_OwnerNotArchived_True() =>
        new Recipe { UserId = Owner }.IsVisibleTo(Owner).Should().BeTrue();

    [Fact]
    public void IsVisibleTo_OtherUser_False() =>
        new Recipe { UserId = Owner }.IsVisibleTo(Guid.NewGuid()).Should().BeFalse();

    [Fact]
    public void IsVisibleTo_Archived_False()
    {
        // Arrange
        var recipe = new Recipe { UserId = Owner };

        // Act
        recipe.Archive(new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc));

        // Assert
        recipe.IsVisibleTo(Owner).Should().BeFalse();
        recipe.ArchivedAtUtc.Should().Be(new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc));
    }
}
