using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.ShoppingList.Dtos;

public sealed record ShoppingListItemDto(
    Guid Id, string Name, double? QuantityGrams, string? QuantityText, FoodCategory Category,
    bool IsPurchased, bool IsManual, Guid? FoodId)
{
    /// <summary>Grams are rounded up to a whole gram: at the shelf, what matters is not buying too little (ADR-008 D6).</summary>
    public static ShoppingListItemDto From(ShoppingListItem item) => new(
        item.Id, item.ItemName, item.QuantityGrams is { } grams ? Math.Ceiling(grams) : null, item.QuantityText,
        item.Category, item.IsPurchased, item.IsManual, item.FoodItemId);
}

public sealed record ShoppingCategoryDto(FoodCategory Category, IReadOnlyList<ShoppingListItemDto> Items);

public sealed record ShoppingListDto(IReadOnlyList<ShoppingCategoryDto> Categories, int TotalCount, int PurchasedCount)
{
    /// <summary>Only categories with items, in aisle order (the enum's), items by name.</summary>
    public static ShoppingListDto From(IReadOnlyCollection<ShoppingListItem> items) => new(
        items
            .GroupBy(i => i.Category)
            .OrderBy(g => g.Key)
            .Select(g => new ShoppingCategoryDto(
                g.Key, g.OrderBy(i => i.ItemName).ThenBy(i => i.Id).Select(ShoppingListItemDto.From).ToList()))
            .ToList(),
        items.Count,
        items.Count(i => i.IsPurchased));
}
