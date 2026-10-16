using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.ShoppingList.Commands.ClearPurchasedItems;

public sealed class ClearPurchasedItemsCommandHandler(IApplicationDbContext db) : IRequestHandler<ClearPurchasedItemsCommand>
{
    public async Task Handle(ClearPurchasedItemsCommand request, CancellationToken cancellationToken)
    {
        var purchased = await db.ShoppingListItems
            .Where(i => i.UserId == request.UserId && i.IsPurchased)
            .ToListAsync(cancellationToken);

        db.ShoppingListItems.RemoveRange(purchased);
        await db.SaveChangesAsync(cancellationToken);
    }
}
