using FluentValidation;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Planner;

namespace NutriScan.Application.ShoppingList.Commands.GenerateShoppingList;

public sealed class GenerateShoppingListCommandValidator : AbstractValidator<GenerateShoppingListCommand>
{
    public GenerateShoppingListCommandValidator(IDateTimeProvider clock) => Include(new PlanRangeValidator(clock));
}
