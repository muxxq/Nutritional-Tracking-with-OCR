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

    // Brute-force protection
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsProfileComplete => HeightCm > 0 && WeightKg > 0;

    public bool IsLockedOut(DateTime nowUtc) => LockoutEndUtc > nowUtc;

    public void RegisterFailedLogin(DateTime nowUtc)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LockoutEndUtc = nowUtc + LockoutDuration;
            FailedLoginAttempts = 0;
        }
    }

    public void RegisterSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
    }

    // Navigation properties
    public ICollection<DiaryEntry> DiaryEntries { get; set; } = new List<DiaryEntry>();
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    public ICollection<PlannedMeal> PlannedMeals { get; set; } = new List<PlannedMeal>();
    public ICollection<ShoppingListItem> ShoppingListItems { get; set; } = new List<ShoppingListItem>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
