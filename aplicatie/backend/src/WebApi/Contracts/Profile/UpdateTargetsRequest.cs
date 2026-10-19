namespace NutriScan.WebApi.Contracts.Profile;

/// <summary>Manual targets; null or missing means "calculate from the profile".</summary>
public sealed record UpdateTargetsRequest(int? Calories, double? ProteinsGrams, double? CarbsGrams, double? FatsGrams);
