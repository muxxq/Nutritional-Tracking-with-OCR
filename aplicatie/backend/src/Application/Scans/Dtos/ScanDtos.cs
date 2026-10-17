using NutriScan.Application.Common.Models;

namespace NutriScan.Application.Scans.Dtos;

/// <summary>A prefilled form field; <see cref="NeedsReview"/> asks the user to confirm it explicitly (FR-05).</summary>
public sealed record ScanFieldDto(double Value, double Confidence, bool NeedsReview);

public sealed record ScanFieldsDto(
    ScanFieldDto? Calories, ScanFieldDto? Proteins, ScanFieldDto? Carbohydrates, ScanFieldDto? Sugars,
    ScanFieldDto? Fats, ScanFieldDto? SaturatedFats, ScanFieldDto? Fiber, ScanFieldDto? SaltGrams)
{
    public static readonly ScanFieldsDto Empty = new(null, null, null, null, null, null, null, null);
}

/// <summary>
/// The prefill for the "Add food" form. With <see cref="OcrStatus.Unavailable"/> every field is null and
/// <see cref="Message"/> tells the user to fill the form by hand (NFR-12).
/// </summary>
public sealed record ScanResultDto(
    OcrStatus Status, string? Message, ServingBase? ServingBase, double? OverallConfidence, ScanFieldsDto Fields);
