using MediatR;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.ShoppingList.Commands.DeleteShoppingListItem;

public sealed class DeleteShoppingListItemCommandHandler(IApplicationDbContext db) : IRequestHandler<DeleteShoppingListItemCommand>
{
    public async Task Handle(DeleteShoppingListItemCommand request, CancellationToken cancellationToken)
    {
        db.ShoppingListItems.Remove(await db.LoadOwnAsync(request.UserId, request.ItemId, cancellationToken));
        await db.SaveOrNotFoundAsync(cancellationToken);
    }
}
