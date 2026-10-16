using FluentValidation;

namespace NutriScan.Application.ShoppingList.Commands.AddShoppingListItem;

public sealed class AddShoppingListItemCommandValidator : AbstractValidator<AddShoppingListItemCommand>
{
    public AddShoppingListItemCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Quantity).MaximumLength(50);
        RuleFor(c => c.Category).IsInEnum().When(c => c.Category is not null);
    }
}
