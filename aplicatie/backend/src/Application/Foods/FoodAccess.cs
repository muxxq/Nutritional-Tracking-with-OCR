using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.Foods;

internal static class FoodAccess
{
    /// <summary>Loads a food the user can see; others' and archived foods are reported as not found.</summary>
    public static async Task<FoodItem> LoadVisibleAsync(this IApplicationDbContext db, Guid userId, Guid foodId, CancellationToken cancellationToken)
    {
        var food = await db.FoodItems.SingleOrDefaultAsync(f => f.Id == foodId, cancellationToken);
        return food is not null && food.IsVisibleTo(userId) ? food : throw new NotFoundException(FoodErrors.NotFound);
    }

    /// <summary>Loads a food the user can change: global foods are visible but read-only.</summary>
    public static async Task<FoodItem> LoadEditableAsync(this IApplicationDbContext db, Guid userId, Guid foodId, CancellationToken cancellationToken)
    {
        var food = await db.LoadVisibleAsync(userId, foodId, cancellationToken);
        return food.IsGlobal ? throw new ForbiddenException(FoodErrors.GlobalReadOnly) : food;
    }

    public static void Apply(this FoodItem food, IFoodFields fields)
    {
        food.Name = fields.Name.Trim();
        food.Brand = string.IsNullOrWhiteSpace(fields.Brand) ? null : fields.Brand.Trim();
        food.Barcode = fields.Barcode;
        food.ServingSizeGrams = fields.ServingSizeGrams;
        food.Calories = fields.Calories;
        food.Proteins = fields.Proteins;
        food.Carbohydrates = fields.Carbohydrates;
        food.Sugars = fields.Sugars;
        food.Fats = fields.Fats;
        food.SaturatedFats = fields.SaturatedFats;
        food.Fiber = fields.Fiber;
        food.SaltGrams = fields.SaltGrams;
        food.Category = fields.Category ?? FoodCategory.Other;
    }
}
