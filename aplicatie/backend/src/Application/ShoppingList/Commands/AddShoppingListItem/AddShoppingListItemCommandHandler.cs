using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.ShoppingList.Dtos;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.ShoppingList.Commands.AddShoppingListItem;

public sealed class AddShoppingListItemCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<AddShoppingListItemCommand, ShoppingListItemDto>
{
    public async Task<ShoppingListItemDto> Handle(AddShoppingListItemCommand request, CancellationToken cancellationToken)
    {
        var item = new ShoppingListItem
        {
            UserId = request.UserId,
            ItemName = request.Name.Trim(),
            QuantityText = string.IsNullOrWhiteSpace(request.Quantity) ? null : request.Quantity.Trim(),
            Category = request.Category ?? FoodCategory.Other,
            CreatedAtUtc = clock.UtcNow
        };

        db.ShoppingListItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        return ShoppingListItemDto.From(item);
    }
}
