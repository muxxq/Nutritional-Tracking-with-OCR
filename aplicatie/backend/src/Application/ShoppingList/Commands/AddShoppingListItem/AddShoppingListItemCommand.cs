using MediatR;
using NutriScan.Application.ShoppingList.Dtos;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.ShoppingList.Commands.AddShoppingListItem;

/// <summary>A line the user adds by hand (UC05 step 7): free-text quantity, never aggregated.</summary>
public sealed record AddShoppingListItemCommand(Guid UserId, string Name, string? Quantity, FoodCategory? Category)
    : IRequest<ShoppingListItemDto>;
