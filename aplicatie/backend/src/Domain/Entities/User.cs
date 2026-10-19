using NutriScan.Domain.Common;
using NutriScan.Domain.Enums;

namespace NutriScan.Domain.Entities;

public class User : BaseEntity, IAuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public Gender Gender { get; set; } = Gender.Male;
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.ModeratelyActive;
    public NutritionGoal Goal { get; set; } = NutritionGoal.MaintainWeight;

    // Manual overrides of the daily targets; null means "calculate from the profile"
    public int? CaloriesOverride { get; set; }
    public double? ProteinsGramsOverride { get; set; }
    public double? CarbsGramsOverride { get; set; }
    public double? FatsGramsOverride { get; set; }

    // Brute-force protection
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsProfileComplete => HeightCm > 0 && WeightKg > 0 && BirthDate is not null;

    /// <summary>Completed years on <paramref name="today"/>; someone born on 29 February ages on 1 March in common years.</summary>
    public int? AgeOn(DateOnly today)
    {
        if (BirthDate is not { } birth)
        {
            return null;
        }

        var age = today.Year - birth.Year;
        return (today.Month, today.Day).CompareTo((birth.Month, birth.Day)) < 0 ? age - 1 : age;
    }

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
