using MediatR;
using NutriScan.Application.ShoppingList.Dtos;

namespace NutriScan.Application.ShoppingList.Commands.SetShoppingListItemPurchased;

public sealed record SetShoppingListItemPurchasedCommand(Guid UserId, Guid ItemId, bool IsPurchased) : IRequest<ShoppingListItemDto>;
