using NutriScan.Domain.Common;
using NutriScan.Domain.Enums;

namespace NutriScan.Domain.Entities;

public class DiaryEntry : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid FoodItemId { get; set; }
    public FoodItem FoodItem { get; set; } = null!;

    public DateOnly ConsumedDate { get; set; }
    public MealType MealType { get; set; }

    public double QuantityGrams { get; set; }

    /// <summary>Servings the user entered, if any; <see cref="QuantityGrams"/> stays the source of truth.</summary>
    public double? Servings { get; set; }

    // Snapshot of the food's values when the entry was saved: later edits to the food don't rewrite history.
    // Stored unrounded so day totals don't accumulate rounding error.
    public double CalculatedCalories { get; set; }
    public double CalculatedProteins { get; set; }
    public double CalculatedCarbs { get; set; }
    public double CalculatedFats { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public void RecalculateNutrients(FoodItem food)
    {
        var ratio = QuantityGrams / 100.0;
        CalculatedCalories = food.Calories * ratio;
        CalculatedProteins = food.Proteins * ratio;
        CalculatedCarbs = food.Carbohydrates * ratio;
        CalculatedFats = food.Fats * ratio;
    }
}
