using FluentValidation;

namespace NutriScan.Application.Foods.Commands.UpdateFood;

public sealed class UpdateFoodCommandValidator : AbstractValidator<UpdateFoodCommand>
{
    public UpdateFoodCommandValidator() => Include(new FoodFieldsValidator());
}
