using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Domain.Nutrition;

/// <summary>
/// Daily nutrition targets from the user's profile and manual overrides (spec FR-02).
/// BMR: Mifflin–St Jeor (1990). Calories: BMR × activity factor × goal factor, with a safety floor.
/// Macros: protein per kg of body weight (ISSN 2017), fat 25% of energy (AMDR), carbohydrates take the rest.
/// Manual overrides are inputs: the targets that depend on them are recalculated around them.
/// </summary>
public static class NutritionTargetCalculator
{
    public const double EnergyTolerance = 0.15;
    public const double FatEnergyShare = 0.25;

    private const double KcalPerGramProtein = 4;
    private const double KcalPerGramCarbs = 4;
    private const double KcalPerGramFat = 9;

    private const int DefaultCalories = 2000;
    private const double DefaultProteinsGrams = 150;
    private const double DefaultCarbsGrams = 200;
    private const double DefaultFatsGrams = 65;

    private const string NegativeCarbsError = "Țintele setate manual nu sunt compatibile: carbohidrații ar deveni negativi.";
    private const string NegativeFatsError = "Țintele setate manual nu sunt compatibile: grăsimile ar deveni negative.";
    private const string InconsistentEnergyError = "Țintele setate manual diferă cu peste 15% de totalul caloric (4P + 4C + 9G).";

    public static NutritionTargetResult Calculate(User user, DateOnly today) =>
        user.IsProfileComplete ? CalculateFromProfile(user, today) : CalculateWithoutProfile(user);

    private static NutritionTargetResult CalculateFromProfile(User user, DateOnly today)
    {
        var age = user.AgeOn(today)!.Value;
        var bmr = 10 * user.WeightKg + 6.25 * user.HeightCm - 5 * age + (user.Gender == Gender.Male ? 5 : -161);
        var tdee = bmr * ActivityFactor(user.ActivityLevel);

        var floor = user.Gender == Gender.Male ? 1500 : 1200;
        var calculatedCalories = Math.Max(floor, Round10(tdee * GoalFactor(user.Goal)));

        var caloriesManual = user.CaloriesOverride is not null;
        double calories = user.CaloriesOverride ?? calculatedCalories;
        var proteins = user.ProteinsGramsOverride ?? user.WeightKg * ProteinGramsPerKg(user.Goal);

        double carbs, fats;
        if (user.CarbsGramsOverride is null)
        {
            fats = user.FatsGramsOverride ?? calories * FatEnergyShare / KcalPerGramFat;
            carbs = (calories - KcalPerGramProtein * proteins - KcalPerGramFat * fats) / KcalPerGramCarbs;
            if (carbs < 0)
            {
                return NutritionTargetResult.Failure(NegativeCarbsError);
            }
        }
        else if (user.FatsGramsOverride is null)
        {
            carbs = user.CarbsGramsOverride.Value;
            fats = (calories - KcalPerGramProtein * proteins - KcalPerGramCarbs * carbs) / KcalPerGramFat;
            if (fats < 0)
            {
                return NutritionTargetResult.Failure(NegativeFatsError);
            }
        }
        else
        {
            carbs = user.CarbsGramsOverride.Value;
            fats = user.FatsGramsOverride.Value;
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
            ProteinsGrams: new TargetValue(RoundToInt(proteins), Source(user.ProteinsGramsOverride is not null)),
            CarbsGrams: new TargetValue(RoundToInt(carbs), Source(user.CarbsGramsOverride is not null)),
            FatsGrams: new TargetValue(RoundToInt(fats), Source(user.FatsGramsOverride is not null))));
    }

    private static NutritionTargetResult CalculateWithoutProfile(User user)
    {
        if (user is { CaloriesOverride: not null, ProteinsGramsOverride: not null, CarbsGramsOverride: not null, FatsGramsOverride: not null }
            && !IsEnergyConsistent(user.CaloriesOverride.Value, user.ProteinsGramsOverride.Value,
                user.CarbsGramsOverride.Value, user.FatsGramsOverride.Value))
        {
            return NutritionTargetResult.Failure(InconsistentEnergyError);
        }

        return NutritionTargetResult.Success(new NutritionTargets(
            Bmr: null,
            Tdee: null,
            Calories: ManualOrDefault(user.CaloriesOverride, DefaultCalories),
            ProteinsGrams: ManualOrDefault(user.ProteinsGramsOverride, DefaultProteinsGrams),
            CarbsGrams: ManualOrDefault(user.CarbsGramsOverride, DefaultCarbsGrams),
            FatsGrams: ManualOrDefault(user.FatsGramsOverride, DefaultFatsGrams)));
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
