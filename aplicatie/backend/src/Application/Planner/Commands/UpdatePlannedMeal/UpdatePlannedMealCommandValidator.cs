using FluentValidation;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Planner.Commands.UpdatePlannedMeal;

public sealed class UpdatePlannedMealCommandValidator : AbstractValidator<UpdatePlannedMealCommand>
{
    public UpdatePlannedMealCommandValidator(IDateTimeProvider clock) => Include(new PlannedMealFieldsValidator(clock));
}
