using NutriScan.Domain.Common;
using NutriScan.Domain.Enums;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Domain.Entities;

/// <summary>
/// A food or a recipe planned for a meal: exactly one of <see cref="FoodItemId"/> / <see cref="RecipeId"/> is set,
/// enforced by CHECK constraints (ADR-007 D1). Values are computed on read, like a recipe's (D2).
/// </summary>
public class PlannedMeal : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateOnly PlanDate { get; set; }
    public MealType MealType { get; set; }

    public Guid? FoodItemId { get; set; }
    public FoodItem? FoodItem { get; set; }

    public Guid? RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    /// <summary>Grams of a planned food; null for a planned recipe, which is planned in servings only.</summary>
    public double? QuantityGrams { get; set; }

    /// <summary>Servings: optional for a food (grams stay the source of truth), required for a recipe.</summary>
    public double? Servings { get; set; }

    /// <summary>The diary entry "I ate this" created; null again if that entry is deleted (ADR-007 D6).</summary>
    public Guid? DiaryEntryId { get; set; }
    public DiaryEntry? DiaryEntry { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsRecipe => RecipeId is not null;

    /// <summary>
    /// Unrounded values from the current food or recipe. The food, or the recipe with its ingredients' foods, must be loaded.
    /// </summary>
    public Macros Nutrients() => IsRecipe
        ? RecipeNutrition.Of(Recipe!).PerServing * Servings!.Value
        : Macros.Of(FoodItem!, QuantityGrams!.Value);
}

/// <summary>
/// A line of the user's shopping list: generated from the plan (a food and unrounded grams) or added by hand (free text),
/// enforced by a CHECK constraint (ADR-008).
/// </summary>
public class ShoppingListItem : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>The food a generated line comes from; null for a line the user added.</summary>
    public Guid? FoodItemId { get; set; }
    public FoodItem? FoodItem { get; set; }

    /// <summary>For a generated line, the food's name when the list was generated: the list is a snapshot (D7).</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Unrounded grams of a generated line; null for a manual one.</summary>
    public double? QuantityGrams { get; set; }

    /// <summary>Free-text quantity of a manual line ("2 bucăți"); null for a generated one.</summary>
    public string? QuantityText { get; set; }

    public FoodCategory Category { get; set; } = FoodCategory.Other;
    public bool IsPurchased { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsManual => FoodItemId is null;
}
