using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Diary;

internal static class DiaryAccess
{
    /// <summary>Loads the user's own entry with its food, even an archived one; other users' entries are not found.</summary>
    public static async Task<DiaryEntry> LoadOwnAsync(this IApplicationDbContext db, Guid userId, Guid entryId, CancellationToken cancellationToken) =>
        await db.DiaryEntries.Include(d => d.FoodItem)
            .SingleOrDefaultAsync(d => d.Id == entryId && d.UserId == userId, cancellationToken)
        ?? throw new NotFoundException(DiaryErrors.NotFound);

    /// <summary>Sets the quantity and takes a fresh nutrient snapshot from the food's current values.</summary>
    public static void Apply(this DiaryEntry entry, IDiaryEntryFields fields, FoodItem food)
    {
        var quantity = DiaryQuantity.Resolve(fields.QuantityGrams, fields.Servings, food.ServingSizeGrams);
        if (!quantity.IsSuccess)
        {
            throw new ValidationException(new[] { new ValidationFailure("Quantity", quantity.Error) });
        }

        entry.ConsumedDate = fields.Date;
        entry.MealType = fields.MealType;
        entry.QuantityGrams = quantity.QuantityGrams;
        entry.Servings = quantity.Servings;
        entry.RecalculateNutrients(food);
    }
}
