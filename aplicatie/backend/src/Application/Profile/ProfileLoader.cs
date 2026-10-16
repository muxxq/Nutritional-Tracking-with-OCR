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

    /// <summary>Reads never fail because of stored overrides: infeasible ones fall back to calculated targets.</summary>
    public static NutritionTargets CalculateTargets(User user, DateOnly today) =>
        NutritionTargetCalculator.CalculateBestEffort(user, today);
}
