using NutriScan.Domain.Enums;

namespace NutriScan.Domain.Nutrition;

public sealed record TargetValue(double Value, TargetSource Source);

public sealed record NutritionTargets(
    int? Bmr,
    int? Tdee,
    TargetValue Calories,
    TargetValue ProteinsGrams,
    TargetValue CarbsGrams,
    TargetValue FatsGrams);

public sealed record NutritionTargetResult(NutritionTargets? Targets, string? Error)
{
    public bool IsValid => Error is null;

    public static NutritionTargetResult Success(NutritionTargets targets) => new(targets, null);

    public static NutritionTargetResult Failure(string error) => new(null, error);
}
