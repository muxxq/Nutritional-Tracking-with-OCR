using NutriScan.Domain.Entities;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Common.Models;

/// <summary>Energy and macronutrients as the API shows them; built unrounded and rounded once, for display.</summary>
public sealed record NutrientsDto(double Calories, double Proteins, double Carbs, double Fats)
{
    public static readonly NutrientsDto Zero = new(0, 0, 0, 0);

    /// <summary>Unrounded snapshot of an entry; sums are built from these and rounded once, for display.</summary>
    public static NutrientsDto Of(DiaryEntry e) => new(e.CalculatedCalories, e.CalculatedProteins, e.CalculatedCarbs, e.CalculatedFats);

    public static NutrientsDto From(Macros m) => new(m.Calories, m.Proteins, m.Carbs, m.Fats);

    public NutrientsDto Rounded() => new(Round(Calories), Round(Proteins), Round(Carbs), Round(Fats));

    public static NutrientsDto operator +(NutrientsDto a, NutrientsDto b) =>
        new(a.Calories + b.Calories, a.Proteins + b.Proteins, a.Carbs + b.Carbs, a.Fats + b.Fats);

    public static NutrientsDto operator -(NutrientsDto a, NutrientsDto b) =>
        new(a.Calories - b.Calories, a.Proteins - b.Proteins, a.Carbs - b.Carbs, a.Fats - b.Fats);

    // "+ 0.0" turns -0 (e.g. 0.03 kcal over target) into 0, which JSON would otherwise print as "-0".
    private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero) + 0.0;
}
