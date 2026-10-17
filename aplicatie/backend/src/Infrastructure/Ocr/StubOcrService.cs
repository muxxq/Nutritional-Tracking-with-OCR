using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Common.Models;

namespace NutriScan.Infrastructure.Ocr;

/// <summary>
/// Stands in for the OCR microservice until Phase 4 (ADR-010 D2): it always reports the engine as unavailable,
/// which is the same degraded path a client must handle when the real service is down (NFR-12).
/// Phase 4 replaces it with an HTTP client (HttpClientFactory + Polly) by changing only its DI registration.
/// </summary>
public sealed class StubOcrService : IOcrService
{
    public Task<OcrResult> ReadLabelAsync(ReadOnlyMemory<byte> image, ImageFormat format, CancellationToken cancellationToken) =>
        Task.FromResult(OcrResult.Unavailable);
}
