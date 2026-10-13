using NutriScan.Domain.Common;

namespace NutriScan.Domain.Entities;

public class FoodItem : BaseEntity, IAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Barcode { get; set; }
    
    // Base nutrition values per 100g
    public double ServingSizeGrams { get; set; } = 100.0;
    public double Calories { get; set; }
    public double Proteins { get; set; }
    public double Carbohydrates { get; set; }
    public double? Sugars { get; set; }
    public double Fats { get; set; }
    public double? SaturatedFats { get; set; }
    public double? Fiber { get; set; }
    public double? Sodium { get; set; } // in milligrams

    public bool IsVerified { get; set; } = false;
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // Navigation properties
    public ICollection<DiaryEntry> DiaryEntries { get; set; } = new List<DiaryEntry>();
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<PlannedMeal> PlannedMeals { get; set; } = new List<PlannedMeal>();
}
