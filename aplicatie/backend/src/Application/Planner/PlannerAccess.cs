using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Planner;

internal static class PlannerAccess
{
    /// <summary>Planned meals with their food, or their recipe and its ingredients' foods, which the values are computed from.</summary>
    public static IQueryable<PlannedMeal> WithItem(this IQueryable<PlannedMeal> meals) =>
        meals
            .Include(m => m.FoodItem)
            .Include(m => m.Recipe!).ThenInclude(r => r.Ingredients).ThenInclude(i => i.FoodItem);

    /// <summary>
    /// Loads the user's own planned meal with its food or recipe, even an archived one (ADR-007 D7); others' are not found.
    /// </summary>
    public static async Task<PlannedMeal> LoadOwnAsync(this IApplicationDbContext db, Guid userId, Guid id, CancellationToken cancellationToken) =>
        await db.PlannedMeals.WithItem().SingleOrDefaultAsync(m => m.Id == id && m.UserId == userId, cancellationToken)
        ?? throw new NotFoundException(PlannerErrors.NotFound);

    /// <summary>
    /// Sets date, meal and quantity. A food takes grams or servings (converted with its serving size, as in the diary);
    /// a recipe takes servings only. The food or recipe must be loaded.
    /// </summary>
    public static void Apply(this PlannedMeal meal, IPlannedMealFields fields)
    {
        if (meal.IsRecipe)
        {
            // The update validator can't know the meal's kind, so grams on a planned recipe are rejected here.
            if (fields.QuantityGrams is not null)
            {
                throw new ValidationException(new[] { new ValidationFailure("Quantity", PlannerErrors.RecipeServingsOnly) });
            }

            meal.QuantityGrams = null;
            meal.Servings = fields.Servings;
        }
        else
        {
            var quantity = DiaryQuantity.Resolve(fields.QuantityGrams, fields.Servings, meal.FoodItem!.ServingSizeGrams);
            if (!quantity.IsSuccess)
            {
                throw new ValidationException(new[] { new ValidationFailure("Quantity", quantity.Error) });
            }

            meal.QuantityGrams = quantity.QuantityGrams;
            meal.Servings = quantity.Servings;
        }

        meal.PlanDate = fields.Date;
        meal.MealType = fields.MealType;
    }
}
