using NutriScan.Domain.Common;
using NutriScan.Domain.Enums;

namespace NutriScan.Domain.Entities;

public class User : BaseEntity, IAuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public Gender Gender { get; set; } = Gender.Male;
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.ModeratelyActive;
    public NutritionGoal Goal { get; set; } = NutritionGoal.MaintainWeight;

    // Daily nutritional targets
    public int TargetCalories { get; set; } = 2000;
    public double TargetProteinsGrams { get; set; } = 150;
    public double TargetCarbsGrams { get; set; } = 200;
    public double TargetFatsGrams { get; set; } = 65;

    // JWT Refresh Token
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // Navigation properties
    public ICollection<DiaryEntry> DiaryEntries { get; set; } = new List<DiaryEntry>();
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    public ICollection<PlannedMeal> PlannedMeals { get; set; } = new List<PlannedMeal>();
    public ICollection<ShoppingListItem> ShoppingListItems { get; set; } = new List<ShoppingListItem>();
}
