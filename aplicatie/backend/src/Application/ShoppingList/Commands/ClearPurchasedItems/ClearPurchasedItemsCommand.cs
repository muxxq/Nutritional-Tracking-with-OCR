using MediatR;

namespace NutriScan.Application.ShoppingList.Commands.ClearPurchasedItems;

/// <summary>"Șterge cele bifate": removes every purchased line of the user's list.</summary>
public sealed record ClearPurchasedItemsCommand(Guid UserId) : IRequest;
