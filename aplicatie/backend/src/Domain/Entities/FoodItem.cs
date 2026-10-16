using NutriScan.Domain.Common;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Domain.Entities;

/// <summary>
/// A food with nutrition values per 100 g. Global foods (no creator) are the verified catalog;
/// foods created by a user are private to that user. Deleting archives, so diary history stays intact.
/// </summary>
public class FoodItem : BaseEntity, IAuditableEntity
{
    private string _name = string.Empty;
    private string? _brand;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            RefreshSearchText();
        }
    }

    public string? Brand
    {
        get => _brand;
        set
        {
            _brand = value;
            RefreshSearchText();
        }
    }

    /// <summary>Lowercase name and brand without diacritics, used for searching.</summary>
    public string SearchText { get; private set; } = string.Empty;

    public string? Barcode { get; set; }

    /// <summary>Typical portion (e.g. 30 g of cereal); nutrition values below are always per 100 g.</summary>
    public double? ServingSizeGrams { get; set; }

    // Nutrition values per 100 g
    public double Calories { get; set; }
    public double Proteins { get; set; }
    public double Carbohydrates { get; set; }
    public double? Sugars { get; set; }
    public double Fats { get; set; }
    public double? SaturatedFats { get; set; }
    public double? Fiber { get; set; }

    /// <summary>Salt in grams, as declared on EU labels (Reg. 1169/2011); sodium = salt / 2.5.</summary>
    public double? SaltGrams { get; set; }

    public bool IsVerified { get; set; } = false;
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime? ArchivedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // Navigation properties
    public ICollection<DiaryEntry> DiaryEntries { get; set; } = new List<DiaryEntry>();
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<PlannedMeal> PlannedMeals { get; set; } = new List<PlannedMeal>();

    public bool IsGlobal => CreatedByUserId is null;

    public bool IsVisibleTo(Guid userId) => ArchivedAtUtc is null && (IsGlobal || CreatedByUserId == userId);

    public void Archive(DateTime nowUtc) => ArchivedAtUtc = nowUtc;

    public string? EnergyWarning() => EnergyCheck.Warning(Calories, Proteins, Carbohydrates, Fats);

    private void RefreshSearchText() => SearchText = TextNormalizer.ForSearch($"{_name} {_brand}");
}
