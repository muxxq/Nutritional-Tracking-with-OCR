namespace NutriScan.Application.Common.Models;

public enum ImageFormat { Jpeg, Png, WebP }

public enum OcrStatus { Processed, Unavailable }

/// <summary>What the label's values refer to: 100 g/ml, or one serving.</summary>
public enum ServingBase { Per100g, PerServing }

/// <summary>A value read from the label and how sure the engine is of it, between 0 and 1.</summary>
public sealed record OcrField(double Value, double Confidence);

/// <summary>The fields of the food form; a null field was not found on the label.</summary>
public sealed record OcrNutrients(
    OcrField? Calories, OcrField? Proteins, OcrField? Carbohydrates, OcrField? Sugars,
    OcrField? Fats, OcrField? SaturatedFats, OcrField? Fiber, OcrField? SaltGrams)
{
    public static readonly OcrNutrients Empty = new(null, null, null, null, null, null, null, null);
}

public sealed record OcrResult(OcrStatus Status, OcrNutrients Nutrients, ServingBase? ServingBase, double? OverallConfidence)
{
    public static readonly OcrResult Unavailable = new(OcrStatus.Unavailable, OcrNutrients.Empty, null, null);
}
