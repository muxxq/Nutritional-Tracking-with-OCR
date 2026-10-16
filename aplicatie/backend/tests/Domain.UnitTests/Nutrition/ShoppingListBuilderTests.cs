using FluentAssertions;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Nutrition;
using Xunit;
using static NutriScan.Domain.UnitTests.Nutrition.RecipeTestData;

namespace NutriScan.Domain.UnitTests.Nutrition;

public class ShoppingListBuilderTests
{
    private static PlannedMeal RecipeMeal(Recipe recipe, double servings) =>
        new() { Recipe = recipe, RecipeId = recipe.Id, Servings = servings };

    private static PlannedMeal FoodMeal(FoodItem food, double grams) =>
        new() { FoodItem = food, FoodItemId = food.Id, QuantityGrams = grams };

    [Fact]
    public void Build_SrsExample_ChickenFromTwoRecipes_Is400()
    {
        // Arrange: FR-12 acceptance criterion
        var chicken = Chicken();
        var meals = new[] { RecipeMeal(Recipe(1, (chicken, 150)), 1), RecipeMeal(Recipe(1, (chicken, 250)), 1) };

        // Act
        var lines = ShoppingListBuilder.Build(meals);

        // Assert
        var line = lines.Should().ContainSingle().Subject;
        line.Food.Should().BeSameAs(chicken);
        line.Grams.Should().Be(400);
    }

    [Fact]
    public void Build_RecipeServings_ScalesIngredients()
    {
        // Arrange: 200 g oats for 4 servings, 2 servings planned
        var recipe = SrsRecipe();

        // Act
        var lines = ShoppingListBuilder.Build([RecipeMeal(recipe, 2)]);

        // Assert
        lines.Should().ContainSingle().Which.Grams.Should().Be(100);
    }

    [Fact]
    public void Build_FoodMeal_UsesGrams()
    {
        // Act
        var lines = ShoppingListBuilder.Build([FoodMeal(Oats(), 150)]);

        // Assert
        lines.Should().ContainSingle().Which.Grams.Should().Be(150);
    }

    [Fact]
    public void Build_SameFoodFromRecipeAndFoodMeal_SumsIntoOneItem()
    {
        // Arrange
        var oats = Oats();
        var recipe = Recipe(4, (oats, 200));

        // Act
        var lines = ShoppingListBuilder.Build([RecipeMeal(recipe, 2), FoodMeal(oats, 50)]);

        // Assert
        lines.Should().ContainSingle().Which.Grams.Should().Be(150);
    }
}
