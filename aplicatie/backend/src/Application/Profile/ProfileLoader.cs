using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Auth;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Profile;

internal static class ProfileLoader
{
    /// <summary>Loads the signed-in user; a token for a deleted account is no longer a valid session.</summary>
    public static async Task<User> LoadUserAsync(this IApplicationDbContext db, Guid userId, CancellationToken cancellationToken) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
        ?? throw new UnauthorizedException(AuthErrors.InvalidSession);

    public static NutritionTargets CalculateTargets(User user, DateOnly today)
    {
        var result = NutritionTargetCalculator.Calculate(user, today);
        return result.Targets
            ?? throw new InvalidOperationException($"Stored targets for user {user.Id} are inconsistent: {result.Error}");
    }
}
