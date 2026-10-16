using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Planner;
using NutriScan.Application.ShoppingList.Dtos;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.ShoppingList.Commands.GenerateShoppingList;

/// <summary>
/// Replaces the generated lines with a fresh snapshot of the plan and keeps the lines the user added (ADR-008 D2).
/// Meals already logged in the diary are left out: they were bought already (D4).
/// </summary>
public sealed class GenerateShoppingListCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<GenerateShoppingListCommand, ShoppingListDto>
{
    public async Task<ShoppingListDto> Handle(GenerateShoppingListCommand request, CancellationToken cancellationToken)
    {
        var meals = await db.PlannedMeals
            .WithItem()
            .Where(m => m.UserId == request.UserId && m.PlanDate >= request.From && m.PlanDate <= request.To
                        && m.DiaryEntryId == null)
            .ToListAsync(cancellationToken);

        // UC05 A1: nothing to buy, so the current list stays as it is (D8).
        if (meals.Count == 0)
        {
            throw new ValidationException(new[] { new ValidationFailure("Range", ShoppingListErrors.NothingPlanned) });
        }

        var items = await db.LoadAllAsync(request.UserId, cancellationToken);
        foreach (var generated in items.Where(i => !i.IsManual).ToList())
        {
            db.ShoppingListItems.Remove(generated);
            items.Remove(generated);
        }

        foreach (var line in ShoppingListBuilder.Build(meals))
        {
            var item = new ShoppingListItem
            {
                UserId = request.UserId,
                FoodItemId = line.Food.Id,
                ItemName = line.Food.Name,
                QuantityGrams = line.Grams,
                Category = line.Food.Category,
                CreatedAtUtc = clock.UtcNow
            };
            db.ShoppingListItems.Add(item);
            items.Add(item);
        }

        // One SaveChanges: the old lines go and the new ones arrive in the same transaction.
        await db.SaveChangesAsync(cancellationToken);

        return ShoppingListDto.From(items);
    }
}
