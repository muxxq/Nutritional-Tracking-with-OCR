using NutriScan.Application.Common.Models;

namespace NutriScan.Application.Scans;

/// <summary>
/// Identifies an image by its leading bytes (magic numbers). The file name and Content-Type come from the client
/// and can lie; the signature is what an image decoder will actually see (NFR-09).
/// </summary>
public static class ImageFormatDetector
{
    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];
    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static ImageFormat? Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(JpegSignature))
        {
            return ImageFormat.Jpeg;
        }

        if (bytes.StartsWith(PngSignature))
        {
            return ImageFormat.Png;
        }

        // RIFF container: "RIFF", 4 bytes of length, then the form type, which must be "WEBP".
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8))
        {
            return ImageFormat.WebP;
        }

        return null;
    }
}
