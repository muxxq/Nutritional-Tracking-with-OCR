using NutriScan.Domain.Common;

namespace NutriScan.Domain.Entities;

/// <summary>
/// A user's private recipe. Its nutrition is computed from the ingredients on every read (ADR-006 D1).
/// Deleting archives, so diary entries logged from it stay intact.
/// </summary>
public class Recipe : BaseEntity, IAuditableEntity
{
    public const int MaxServings = 50;

    private string _name = string.Empty;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            SearchText = TextNormalizer.ForSearch(value);
        }
    }

    /// <summary>Lowercase name without diacritics, used for searching.</summary>
    public string SearchText { get; private set; } = string.Empty;

    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int ServingsCount { get; set; } = 1;
    public int? PrepTimeMinutes { get; set; }

    public DateTime? ArchivedAtUtc { get; private set; }

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<PlannedMeal> PlannedMeals { get; set; } = new List<PlannedMeal>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsVisibleTo(Guid userId) => ArchivedAtUtc is null && UserId == userId;

    public void Archive(DateTime nowUtc) => ArchivedAtUtc = nowUtc;
}

public class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public Guid FoodItemId { get; set; }
    public FoodItem FoodItem { get; set; } = null!;

    public double QuantityGrams { get; set; }
}
