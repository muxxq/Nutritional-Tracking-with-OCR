namespace NutriScan.WebApi.Contracts.ShoppingList;

/// <summary>Checks or unchecks a line; the value is required.</summary>
public sealed record SetPurchasedRequest(bool? IsPurchased);
