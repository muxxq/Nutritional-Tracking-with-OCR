using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.Planner.Commands.EatPlannedMeal;

/// <summary>
/// Creates a diary entry on the planned day and meal, with the planned quantity and a snapshot of the current values
/// (ADR-007 D5). The plan keeps the meal and points to the entry, so it can't be logged twice (D6). Built directly
/// rather than through AddDiaryEntry, which refuses archived items: a meal planned before archiving can still be eaten (D7).
/// </summary>
public sealed class EatPlannedMealCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<EatPlannedMealCommand, DiaryEntryDto>
{
    public async Task<DiaryEntryDto> Handle(EatPlannedMealCommand request, CancellationToken cancellationToken)
    {
        var meal = await db.LoadOwnAsync(request.UserId, request.PlannedMealId, cancellationToken);

        if (meal.DiaryEntryId is not null)
        {
            throw new ConflictException(PlannerErrors.AlreadyEaten);
        }

        // Same limit as the diary: one day of slack for time zones east of UTC.
        if (meal.PlanDate > DateOnly.FromDateTime(clock.UtcNow).AddDays(1))
        {
            throw new ValidationException(new[] { new ValidationFailure("Date", PlannerErrors.FutureMeal) });
        }

        var entry = new DiaryEntry
        {
            UserId = meal.UserId,
            FoodItemId = meal.FoodItemId,
            FoodItem = meal.FoodItem,
            RecipeId = meal.RecipeId,
            Recipe = meal.Recipe,
            ConsumedDate = meal.PlanDate,
            MealType = meal.MealType,
            QuantityGrams = meal.QuantityGrams,
            Servings = meal.Servings,
            CreatedAtUtc = clock.UtcNow
        };

        if (meal.IsRecipe)
        {
            entry.RecalculateNutrients(meal.Recipe!);
        }
        else
        {
            entry.RecalculateNutrients(meal.FoodItem!);
        }

        db.DiaryEntries.Add(entry);
        meal.DiaryEntry = entry;
        meal.DiaryEntryId = entry.Id;
        meal.UpdatedAtUtc = clock.UtcNow;
        // One SaveChanges: the entry and the link are written in the same transaction.
        await db.SaveChangesAsync(cancellationToken);

        return DiaryEntryDto.From(entry);
    }
}
