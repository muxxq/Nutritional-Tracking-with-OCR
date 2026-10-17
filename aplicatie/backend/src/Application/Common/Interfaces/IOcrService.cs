using NutriScan.Application.Common.Models;

namespace NutriScan.Application.Common.Interfaces;

/// <summary>
/// Port to the label-reading engine (ADR-010). It speaks the food form's language: translating the OCR microservice's
/// JSON is the adapter's job, so a change in the Python service stops at Infrastructure.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Reads a nutrition label held in memory; the image is never stored (SRS §6.4).
    /// An unreachable or failing engine is reported as <see cref="OcrResult.Unavailable"/>, not thrown (NFR-12).
    /// </summary>
    Task<OcrResult> ReadLabelAsync(ReadOnlyMemory<byte> image, ImageFormat format, CancellationToken cancellationToken);
}
