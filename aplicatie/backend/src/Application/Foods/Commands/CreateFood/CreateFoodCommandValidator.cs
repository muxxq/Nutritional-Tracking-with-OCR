using FluentValidation;

namespace NutriScan.Application.Foods.Commands.CreateFood;

public sealed class CreateFoodCommandValidator : AbstractValidator<CreateFoodCommand>
{
    public CreateFoodCommandValidator() => Include(new FoodFieldsValidator());
}
