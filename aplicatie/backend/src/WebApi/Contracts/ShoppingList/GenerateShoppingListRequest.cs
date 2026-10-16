namespace NutriScan.WebApi.Contracts.ShoppingList;

/// <summary>The range of plan days to buy for (at most 31).</summary>
public sealed record GenerateShoppingListRequest(DateOnly From, DateOnly To);
