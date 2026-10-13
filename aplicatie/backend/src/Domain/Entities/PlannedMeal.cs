using NutriScan.Domain.Common;
using NutriScan.Domain.Enums;

namespace NutriScan.Domain.Entities;

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

    public double QuantityGrams { get; set; }
    public bool IsTransferredToDiary { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public class ShoppingListItem : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string ItemName { get; set; } = string.Empty;
    public double QuantityGrams { get; set; }
    public bool IsPurchased { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
