using NutriScan.Domain.Enums;

namespace NutriScan.WebApi.Contracts.ShoppingList;

/// <summary>A line added by hand: a name, an optional free-text quantity and an aisle.</summary>
public sealed record AddShoppingListItemRequest(string Name, string? Quantity, FoodCategory? Category);
