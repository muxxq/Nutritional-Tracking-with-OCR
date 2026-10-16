using NutriScan.Domain.Common;
using NutriScan.Domain.Enums;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Domain.Entities;

/// <summary>
/// A food or a recipe eaten at a meal: exactly one of <see cref="FoodItemId"/> / <see cref="RecipeId"/> is set,
/// enforced by CHECK constraints in the database (ADR-006 D2). Recipes are logged in servings only (D3).
/// </summary>
public class DiaryEntry : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? FoodItemId { get; set; }
    public FoodItem? FoodItem { get; set; }

    public Guid? RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    public DateOnly ConsumedDate { get; set; }
    public MealType MealType { get; set; }

    /// <summary>Grams of a food entry; null for a recipe entry, whose raw ingredient weight would mislead.</summary>
    public double? QuantityGrams { get; set; }

    /// <summary>
    /// Servings the user entered: optional for a food (grams stay the source of truth), required for a recipe.
    /// </summary>
    public double? Servings { get; set; }

    // Snapshot of the food's values when the entry was saved: later edits to the food don't rewrite history.
    // Stored unrounded so day totals don't accumulate rounding error.
    public double CalculatedCalories { get; set; }
    public double CalculatedProteins { get; set; }
    public double CalculatedCarbs { get; set; }
    public double CalculatedFats { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsRecipe => RecipeId is not null;

    public void RecalculateNutrients(FoodItem food)
    {
        var ratio = QuantityGrams!.Value / 100.0;
        CalculatedCalories = food.Calories * ratio;
        CalculatedProteins = food.Proteins * ratio;
        CalculatedCarbs = food.Carbohydrates * ratio;
        CalculatedFats = food.Fats * ratio;
    }

    /// <summary>Snapshot of <see cref="Servings"/> servings; the recipe must be loaded with its ingredients' foods.</summary>
    public void RecalculateNutrients(Recipe recipe)
    {
        var m = RecipeNutrition.Of(recipe).PerServing * Servings!.Value;
        CalculatedCalories = m.Calories;
        CalculatedProteins = m.Proteins;
        CalculatedCarbs = m.Carbs;
        CalculatedFats = m.Fats;
    }
}
