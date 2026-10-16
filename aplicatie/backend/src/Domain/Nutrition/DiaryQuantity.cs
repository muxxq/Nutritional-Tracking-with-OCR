namespace NutriScan.Domain.Nutrition;

public sealed record DiaryQuantityResult(double QuantityGrams, double? Servings, string? Error)
{
    public bool IsSuccess => Error is null;

    public static DiaryQuantityResult Failure(string error) => new(0, null, error);
}

/// <summary>
/// Turns a diary quantity given in grams or in servings into grams, the unit nutrients are scaled by.
/// The serving count is kept only so the client can show "1.5 servings (60 g)" again when editing.
/// </summary>
public static class DiaryQuantity
{
    public const double MaxGrams = 5000;
    public const double MaxServings = 50;

    public const string ExactlyOneQuantity = "Introdu fie cantitatea în grame, fie numărul de porții.";
    public const string NoServingSize = "Alimentul nu are o porție definită; introdu cantitatea în grame.";
    public const string TooMuch = "Cantitatea totală nu poate depăși 5000 g.";

    public static DiaryQuantityResult Resolve(double? grams, double? servings, double? servingSizeGrams)
    {
        if (grams.HasValue == servings.HasValue)
        {
            return DiaryQuantityResult.Failure(ExactlyOneQuantity);
        }

        if (grams is { } g)
        {
            return new DiaryQuantityResult(g, null, null);
        }

        if (servingSizeGrams is not { } servingSize)
        {
            return DiaryQuantityResult.Failure(NoServingSize);
        }

        var total = servings!.Value * servingSize;
        return total > MaxGrams
            ? DiaryQuantityResult.Failure(TooMuch)
            : new DiaryQuantityResult(total, servings, null);
    }
}
