using MediatR;

namespace NutriScan.Application.ShoppingList.Commands.DeleteShoppingListItem;

public sealed record DeleteShoppingListItemCommand(Guid UserId, Guid ItemId) : IRequest;
