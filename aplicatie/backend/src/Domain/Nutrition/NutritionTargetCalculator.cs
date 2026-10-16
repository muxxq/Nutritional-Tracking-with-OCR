using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Domain.Nutrition;

/// <summary>
/// Daily nutrition targets from the user's profile and manual overrides (spec FR-02).
/// BMR: Mifflin–St Jeor (1990). Calories: BMR × activity factor × goal factor, with a safety floor.
/// Macros: protein per kg of body weight, capped at BMI 25 (ISSN 2017), fat 25% of energy (AMDR), carbohydrates take the rest.
/// Manual overrides are inputs: the targets that depend on them are recalculated around them.
/// </summary>
public static class NutritionTargetCalculator
{
    public const double EnergyTolerance = 0.15;
    public const double FatEnergyShare = 0.25;
    public const double ReferenceBmi = 25;

    private const double KcalPerGramProtein = 4;
    private const double KcalPerGramCarbs = 4;
    private const double KcalPerGramFat = 9;

    private const int DefaultCalories = 2000;
    private const double DefaultProteinsGrams = 150;
    private const double DefaultCarbsGrams = 200;
    private const double DefaultFatsGrams = 65;

    private const string NegativeCarbsError = "Țintele setate manual nu sunt compatibile: carbohidrații ar deveni negativi.";
    private const string NegativeFatsError = "Țintele setate manual nu sunt compatibile: grăsimile ar deveni negative.";
    private const string ExceedingMacrosError = "Macronutrienții setați manual depășesc caloriile țintă cu peste 15%.";
    private const string InconsistentEnergyError = "Țintele setate manual diferă cu peste 15% de totalul caloric (4P + 4C + 9G).";

    /// <summary>Strict: used when writing. Infeasible overrides are reported as an error.</summary>
    public static NutritionTargetResult Calculate(User user, DateOnly today) =>
        Calculate(user, today, TargetOverrides.From(user));

    /// <summary>
    /// Lenient: used when reading. Overrides that were valid when saved can become infeasible later
    /// (e.g. a birthday lowers BMR); then the targets are calculated as if no overrides were set.
    /// The user's stored overrides are not changed.
    /// </summary>
    public static NutritionTargets CalculateBestEffort(User user, DateOnly today) =>
        Calculate(user, today).Targets
        ?? Calculate(user, today, TargetOverrides.None).Targets
        ?? throw new InvalidOperationException("Targets without overrides are always feasible.");

    private static NutritionTargetResult Calculate(User user, DateOnly today, TargetOverrides overrides) =>
        user.IsProfileComplete ? CalculateFromProfile(user, today, overrides) : CalculateWithoutProfile(overrides);

    private sealed record TargetOverrides(int? Calories, double? ProteinsGrams, double? CarbsGrams, double? FatsGrams)
    {
        public static readonly TargetOverrides None = new(null, null, null, null);

        public static TargetOverrides From(User user) =>
            new(user.CaloriesOverride, user.ProteinsGramsOverride, user.CarbsGramsOverride, user.FatsGramsOverride);
    }

    private static NutritionTargetResult CalculateFromProfile(User user, DateOnly today, TargetOverrides overrides)
    {
        var age = user.AgeOn(today)!.Value;
        var bmr = 10 * user.WeightKg + 6.25 * user.HeightCm - 5 * age + (user.Gender == Gender.Male ? 5 : -161);
        var tdee = bmr * ActivityFactor(user.ActivityLevel);

        var floor = user.Gender == Gender.Male ? 1500 : 1200;
        var calculatedCalories = Math.Max(floor, Round10(tdee * GoalFactor(user.Goal)));

        var caloriesManual = overrides.Calories is not null;
        double calories = overrides.Calories ?? calculatedCalories;
        var proteins = overrides.ProteinsGrams ?? ProteinReferenceWeightKg(user) * ProteinGramsPerKg(user.Goal);

        double carbs, fats;
        if (overrides.CarbsGrams is null)
        {
            fats = overrides.FatsGrams ?? calories * FatEnergyShare / KcalPerGramFat;
            carbs = (calories - KcalPerGramProtein * proteins - KcalPerGramFat * fats) / KcalPerGramCarbs;
            if (carbs < 0)
            {
                return NutritionTargetResult.Failure(NegativeCarbsError);
            }
        }
        else if (overrides.FatsGrams is null)
        {
            carbs = overrides.CarbsGrams.Value;
            fats = (calories - KcalPerGramProtein * proteins - KcalPerGramCarbs * carbs) / KcalPerGramFat;
            if (fats < 0)
            {
                return NutritionTargetResult.Failure(NegativeFatsError);
            }
        }
        else
        {
            carbs = overrides.CarbsGrams.Value;
            fats = overrides.FatsGrams.Value;
            if (caloriesManual)
            {
                if (!IsEnergyConsistent(calories, proteins, carbs, fats))
                {
                    return NutritionTargetResult.Failure(InconsistentEnergyError);
                }
            }
            else
            {
                // Every macro is chosen by the user: calories follow from them, no safety floor.
                calories = Round10(Energy(proteins, carbs, fats));
            }
        }

        return NutritionTargetResult.Success(new NutritionTargets(
            Bmr: RoundToInt(bmr),
            Tdee: RoundToInt(tdee),
            Calories: new TargetValue(calories, Source(caloriesManual)),
            ProteinsGrams: new TargetValue(RoundToInt(proteins), Source(overrides.ProteinsGrams is not null)),
            CarbsGrams: new TargetValue(RoundToInt(carbs), Source(overrides.CarbsGrams is not null)),
            FatsGrams: new TargetValue(RoundToInt(fats), Source(overrides.FatsGrams is not null))));
    }

    private static NutritionTargetResult CalculateWithoutProfile(TargetOverrides overrides)
    {
        if (overrides is { Calories: not null, ProteinsGrams: not null, CarbsGrams: not null, FatsGrams: not null }
            && !IsEnergyConsistent(overrides.Calories.Value, overrides.ProteinsGrams.Value,
                overrides.CarbsGrams.Value, overrides.FatsGrams.Value))
        {
            return NutritionTargetResult.Failure(InconsistentEnergyError);
        }

        // Without a profile nothing absorbs the difference, so manual macros alone must fit the calories.
        var manualMacroEnergy = Energy(overrides.ProteinsGrams ?? 0, overrides.CarbsGrams ?? 0, overrides.FatsGrams ?? 0);
        if (manualMacroEnergy > (overrides.Calories ?? DefaultCalories) * (1 + EnergyTolerance))
        {
            return NutritionTargetResult.Failure(ExceedingMacrosError);
        }

        return NutritionTargetResult.Success(new NutritionTargets(
            Bmr: null,
            Tdee: null,
            Calories: ManualOrDefault(overrides.Calories, DefaultCalories),
            ProteinsGrams: ManualOrDefault(overrides.ProteinsGrams, DefaultProteinsGrams),
            CarbsGrams: ManualOrDefault(overrides.CarbsGrams, DefaultCarbsGrams),
            FatsGrams: ManualOrDefault(overrides.FatsGrams, DefaultFatsGrams)));
    }

    /// <summary>
    /// Body weight capped at BMI 25 for the height. Protein needs follow lean mass, so total weight
    /// overestimates them in obesity (and would leave no energy for carbohydrates at the extremes).
    /// </summary>
    private static double ProteinReferenceWeightKg(User user)
    {
        var heightM = user.HeightCm / 100;
        return Math.Min(user.WeightKg, ReferenceBmi * heightM * heightM);
    }

    private static double Energy(double proteins, double carbs, double fats) =>
        KcalPerGramProtein * proteins + KcalPerGramCarbs * carbs + KcalPerGramFat * fats;

    private static bool IsEnergyConsistent(double calories, double proteins, double carbs, double fats) =>
        Math.Abs(Energy(proteins, carbs, fats) - calories) / calories <= EnergyTolerance;

    private static TargetValue ManualOrDefault(double? manual, double fallback) =>
        manual is null ? new TargetValue(fallback, TargetSource.Default) : new TargetValue(manual.Value, TargetSource.Manual);

    private static TargetSource Source(bool manual) => manual ? TargetSource.Manual : TargetSource.Calculated;

    private static int Round10(double value) => (int)Math.Round(value / 10, MidpointRounding.AwayFromZero) * 10;

    private static int RoundToInt(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static double ActivityFactor(ActivityLevel level) => level switch
    {
        ActivityLevel.Sedentary => 1.2,
        ActivityLevel.LightlyActive => 1.375,
        ActivityLevel.ModeratelyActive => 1.55,
        ActivityLevel.VeryActive => 1.725,
        ActivityLevel.ExtremelyActive => 1.9,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };

    private static double GoalFactor(NutritionGoal goal) => goal switch
    {
        NutritionGoal.LoseWeight => 0.80,
        NutritionGoal.MaintainWeight => 1.00,
        NutritionGoal.GainMuscle => 1.10,
        _ => throw new ArgumentOutOfRangeException(nameof(goal), goal, null)
    };

    private static double ProteinGramsPerKg(NutritionGoal goal) => goal switch
    {
        NutritionGoal.LoseWeight => 2.0,
        NutritionGoal.MaintainWeight => 1.6,
        NutritionGoal.GainMuscle => 1.8,
        _ => throw new ArgumentOutOfRangeException(nameof(goal), goal, null)
    };
}
