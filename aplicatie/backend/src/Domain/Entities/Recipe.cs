using NutriScan.Domain.Common;

namespace NutriScan.Domain.Entities;

public class Recipe : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int ServingsCount { get; set; } = 1;

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<PlannedMeal> PlannedMeals { get; set; } = new List<PlannedMeal>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public Guid FoodItemId { get; set; }
    public FoodItem FoodItem { get; set; } = null!;

    public double QuantityGrams { get; set; }
}
