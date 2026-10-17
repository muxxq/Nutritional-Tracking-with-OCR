using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Profile;
using NutriScan.Application.Scans.Dtos;

namespace NutriScan.Application.Scans.Commands.ScanLabel;

public sealed class ScanLabelCommandHandler(IApplicationDbContext db, IOcrService ocr)
    : IRequestHandler<ScanLabelCommand, ScanResultDto>
{
    /// <summary>
    /// FR-05: below this confidence a field must be confirmed by the user. One server-side constant, so every client
    /// applies the same rule and it can be calibrated against the engine's measured accuracy in Phase 4.
    /// </summary>
    public const double ReviewThreshold = 0.80;

    public async Task<ScanResultDto> Handle(ScanLabelCommand request, CancellationToken cancellationToken)
    {
        await db.LoadUserAsync(request.UserId, cancellationToken);

        // The validator has already accepted the signature.
        var format = ImageFormatDetector.Detect(request.Image)!.Value;
        var result = await ocr.ReadLabelAsync(request.Image, format, cancellationToken);

        if (result.Status == OcrStatus.Unavailable)
        {
            return new ScanResultDto(OcrStatus.Unavailable, ScanErrors.OcrUnavailable, null, null, ScanFieldsDto.Empty);
        }

        var n = result.Nutrients;
        return new ScanResultDto(
            OcrStatus.Processed, null, result.ServingBase, result.OverallConfidence,
            new ScanFieldsDto(
                Field(n.Calories), Field(n.Proteins), Field(n.Carbohydrates), Field(n.Sugars),
                Field(n.Fats), Field(n.SaturatedFats), Field(n.Fiber), Field(n.SaltGrams)));
    }

    private static ScanFieldDto? Field(OcrField? f) =>
        f is null ? null : new ScanFieldDto(f.Value, f.Confidence, f.Confidence < ReviewThreshold);
}
