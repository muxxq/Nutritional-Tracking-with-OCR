using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Profile.Dtos;

public sealed record TargetDto(double Value, TargetSource Source)
{
    public static TargetDto From(TargetValue value) => new(value.Value, value.Source);
}

public sealed record TargetsDto(TargetDto Calories, TargetDto ProteinsGrams, TargetDto CarbsGrams, TargetDto FatsGrams);

public sealed record ProfileDto(
    string Email,
    string FirstName,
    string LastName,
    DateOnly? BirthDate,
    int? Age,
    double HeightCm,
    double WeightKg,
    Gender Gender,
    ActivityLevel ActivityLevel,
    NutritionGoal Goal,
    bool IsProfileComplete,
    int? Bmr,
    int? Tdee,
    TargetsDto Targets)
{
    public static ProfileDto From(User user, NutritionTargets targets, DateOnly today) => new(
        user.Email,
        user.FirstName,
        user.LastName,
        user.BirthDate,
        user.AgeOn(today),
        user.HeightCm,
        user.WeightKg,
        user.Gender,
        user.ActivityLevel,
        user.Goal,
        user.IsProfileComplete,
        targets.Bmr,
        targets.Tdee,
        new TargetsDto(
            TargetDto.From(targets.Calories),
            TargetDto.From(targets.ProteinsGrams),
            TargetDto.From(targets.CarbsGrams),
            TargetDto.From(targets.FatsGrams)));
}
