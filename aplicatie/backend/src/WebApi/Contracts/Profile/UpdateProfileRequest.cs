using NutriScan.Domain.Enums;

namespace NutriScan.WebApi.Contracts.Profile;

public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    DateOnly BirthDate,
    double HeightCm,
    double WeightKg,
    Gender Gender,
    ActivityLevel ActivityLevel,
    NutritionGoal Goal);
