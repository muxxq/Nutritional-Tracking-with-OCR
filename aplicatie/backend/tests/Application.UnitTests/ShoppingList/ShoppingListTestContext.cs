using NutriScan.Application.Planner.Commands.CreatePlannedMeal;
using NutriScan.Application.Recipes.Commands.CreateRecipe;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Application.UnitTests.Recipes;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.ShoppingList;

/// <summary>Arranges foods, recipes and planned meals for the shopping list tests.</summary>
public sealed class ShoppingListTestContext : IDisposable
{
    public static readonly DateOnly Monday = new(2026, 10, 19);

    public AuthTestContext Ctx { get; } = new();
    public Guid Me { get; } = Guid.NewGuid();

    public async Task<FoodItem> FoodAsync(string name, FoodCategory category, Guid? owner = null)
    {
        var food = new FoodItem
        {
            Name = name, CreatedByUserId = owner, Category = category, Calories = 100, Proteins = 10, Carbohydrates = 10, Fats = 1
        };
        Ctx.Db.FoodItems.Add(food);
        await Ctx.Db.SaveChangesAsync();
        return food;
    }

    public async Task<Guid> RecipeAsync(int servings, params (Guid FoodId, double Grams)[] ingredients) =>
        (await new CreateRecipeCommandHandler(Ctx.Db, Ctx.Clock.Object).Handle(
            RecipeTestData.Create(Me, ingredients) with { ServingsCount = servings }, CancellationToken.None)).Id;

    public async Task<Guid> PlanRecipeAsync(Guid recipeId, double servings, DateOnly? date = null, Guid? userId = null) =>
        (await new CreatePlannedMealCommandHandler(Ctx.Db, Ctx.Clock.Object).Handle(
            new CreatePlannedMealCommand(userId ?? Me, null, recipeId, date ?? Monday, MealType.Lunch, null, servings),
            CancellationToken.None)).Id;

    public async Task<Guid> PlanFoodAsync(Guid foodId, double grams, DateOnly? date = null, Guid? userId = null) =>
        (await new CreatePlannedMealCommandHandler(Ctx.Db, Ctx.Clock.Object).Handle(
            new CreatePlannedMealCommand(userId ?? Me, foodId, null, date ?? Monday, MealType.Dinner, grams, null),
            CancellationToken.None)).Id;

    public void Dispose() => Ctx.Dispose();
}
