using FluentValidation;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Planner.Commands.CreatePlannedMeal;

public sealed class CreatePlannedMealCommandValidator : AbstractValidator<CreatePlannedMealCommand>
{
    public CreatePlannedMealCommandValidator(IDateTimeProvider clock)
    {
        Include(new PlannedMealFieldsValidator(clock));

        RuleFor(c => c)
            .Must(c => c.FoodId.HasValue != c.RecipeId.HasValue)
            .OverridePropertyName("Item")
            .WithMessage(PlannerErrors.ExactlyOneItem);
        RuleFor(c => c.QuantityGrams)
            .Null().When(c => c.RecipeId is not null)
            .OverridePropertyName("Quantity")
            .WithMessage(PlannerErrors.RecipeServingsOnly);
    }
}
