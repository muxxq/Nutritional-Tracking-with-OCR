using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.ShoppingList.Dtos;

namespace NutriScan.Application.ShoppingList.Commands.SetShoppingListItemPurchased;

public sealed class SetShoppingListItemPurchasedCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<SetShoppingListItemPurchasedCommand, ShoppingListItemDto>
{
    public async Task<ShoppingListItemDto> Handle(SetShoppingListItemPurchasedCommand request, CancellationToken cancellationToken)
    {
        var item = await db.LoadOwnAsync(request.UserId, request.ItemId, cancellationToken);
        item.IsPurchased = request.IsPurchased;
        item.UpdatedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ShoppingListItemDto.From(item);
    }
}
