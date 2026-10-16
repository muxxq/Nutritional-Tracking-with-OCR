using NutriScan.Application.Profile.Commands.UpdateProfile;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Profile;

public static class ProfileTestData
{
    /// <summary>Reference case R1 (spec §3.6) for a user on 2026-10-16: 28 years old.</summary>
    public static UpdateProfileCommand R1(Guid userId) => new(
        userId, "Ana", "Pop", new DateOnly(1998, 5, 14), 168, 62,
        Gender.Female, ActivityLevel.ModeratelyActive, NutritionGoal.LoseWeight);
}
