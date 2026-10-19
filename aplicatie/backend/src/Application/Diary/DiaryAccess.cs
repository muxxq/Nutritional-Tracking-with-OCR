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

    /// <summary>Entries with their food, or their recipe and its ingredients' foods (needed to recalculate the snapshot).</summary>
    internal static IQueryable<DiaryEntry> WithItem(this IQueryable<DiaryEntry> entries) =>
        entries
            .Include(d => d.FoodItem)
            .Include(d => d.Recipe!).ThenInclude(r => r.Ingredients).ThenInclude(i => i.FoodItem);

    /// <summary>
    /// Loads the user's own entry with its food or recipe, even an archived one; other users' entries are not found.
    /// </summary>
    internal static async Task<DiaryEntry> LoadOwnAsync(this IApplicationDbContext db, Guid userId, Guid entryId, CancellationToken cancellationToken) =>
        await db.DiaryEntries.WithItem()
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

    /// <summary>
    /// Sets the servings and takes a fresh snapshot from the recipe's current values. A recipe is logged in servings only:
    /// its raw ingredient weight is not what ends up on the plate (ADR-006 D3).
    /// </summary>
    internal static void Apply(this DiaryEntry entry, IDiaryEntryFields fields, Recipe recipe)
    {
        // The update validator can't know the entry's kind, so grams on a recipe entry are rejected here.
        if (fields.QuantityGrams is not null)
        {
            throw new ValidationException(new[] { new ValidationFailure("Quantity", DiaryErrors.RecipeServingsOnly) });
        }

        entry.ConsumedDate = fields.Date;
        entry.MealType = fields.MealType;
        entry.QuantityGrams = null;
        entry.Servings = fields.Servings;
        entry.RecalculateNutrients(recipe);
    }
}
