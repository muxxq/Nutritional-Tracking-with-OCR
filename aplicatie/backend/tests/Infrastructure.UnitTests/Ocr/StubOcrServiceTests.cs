using FluentAssertions;
using NutriScan.Application.Common.Models;
using NutriScan.Infrastructure.Ocr;

namespace NutriScan.Infrastructure.UnitTests.Ocr;

public class StubOcrServiceTests
{
    [Fact]
    public async Task ReadLabel_AlwaysReportsUnavailable()
    {
        // Act
        var result = await new StubOcrService().ReadLabelAsync(new byte[] { 0xFF, 0xD8, 0xFF }, ImageFormat.Jpeg, CancellationToken.None);

        // Assert
        result.Should().Be(OcrResult.Unavailable);
        result.Nutrients.Should().Be(OcrNutrients.Empty);
    }
}
