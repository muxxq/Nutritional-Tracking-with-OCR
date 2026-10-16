using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Diary;

public static class DiaryAccess
{
    /// <summary>Saves an entry change; an entry deleted by a concurrent request is reported as not found.</summary>
    public static async Task SaveOrNotFoundAsync(this IApplicationDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another tab or a double tap deleted the entry between our read and this write: 0 rows affected.
            throw new NotFoundException(DiaryErrors.NotFound);
        }
    }

    /// <summary>Loads the user's own entry with its food, even an archived one; other users' entries are not found.</summary>
    internal static async Task<DiaryEntry> LoadOwnAsync(this IApplicationDbContext db, Guid userId, Guid entryId, CancellationToken cancellationToken) =>
        await db.DiaryEntries.Include(d => d.FoodItem)
            .SingleOrDefaultAsync(d => d.Id == entryId && d.UserId == userId, cancellationToken)
        ?? throw new NotFoundException(DiaryErrors.NotFound);

    /// <summary>Sets the quantity and takes a fresh nutrient snapshot from the food's current values.</summary>
    internal static void Apply(this DiaryEntry entry, IDiaryEntryFields fields, FoodItem food)
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
