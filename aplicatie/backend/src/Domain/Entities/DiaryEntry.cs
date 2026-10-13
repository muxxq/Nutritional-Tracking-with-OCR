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
    
    // Cached calculated values for high performance querying
    public double CalculatedCalories { get; set; }
    public double CalculatedProteins { get; set; }
    public double CalculatedCarbs { get; set; }
    public double CalculatedFats { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public void RecalculateNutrients()
    {
        if (FoodItem == null) return;
        var ratio = QuantityGrams / 100.0;
        CalculatedCalories = Math.Round(FoodItem.Calories * ratio, 1);
        CalculatedProteins = Math.Round(FoodItem.Proteins * ratio, 1);
        CalculatedCarbs = Math.Round(FoodItem.Carbohydrates * ratio, 1);
        CalculatedFats = Math.Round(FoodItem.Fats * ratio, 1);
    }
}
