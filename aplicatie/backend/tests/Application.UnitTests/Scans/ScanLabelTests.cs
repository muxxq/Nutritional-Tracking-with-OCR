using System.Text;
using FluentAssertions;
using Moq;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Scans;
using NutriScan.Application.Scans.Commands.ScanLabel;
using NutriScan.Application.Scans.Dtos;
using NutriScan.Application.UnitTests.Auth;

namespace NutriScan.Application.UnitTests.Scans;

public class ScanLabelTests : IDisposable
{
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    public static readonly byte[] WebP = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBPVP8 "u8];

    private readonly AuthTestContext _ctx = new();
    private readonly Mock<IOcrService> _ocr = new();
    private readonly ScanLabelCommandValidator _validator = new();

    private static OcrResult Processed(double proteinsConfidence) => new(
        OcrStatus.Processed,
        OcrNutrients.Empty with
        {
            Calories = new OcrField(389, 0.95),
            Proteins = new OcrField(16.9, proteinsConfidence),
            Fats = new OcrField(6.9, ScanLabelCommandHandler.ReviewThreshold)
        },
        ServingBase.Per100g,
        0.86);

    private async Task<ScanResultDto> ScanAsync(byte[] image)
    {
        var user = await _ctx.SeedUserAsync();
        return await new ScanLabelCommandHandler(_ctx.Db, _ocr.Object)
            .Handle(new ScanLabelCommand(user.Id, image), CancellationToken.None);
    }

    [Theory]
    [MemberData(nameof(Signatures))]
    public void Detect_KnownSignatures(byte[] bytes, ImageFormat? expected)
    {
        // Act
        var format = ImageFormatDetector.Detect(bytes);

        // Assert
        format.Should().Be(expected);
    }

    public static TheoryData<byte[], ImageFormat?> Signatures => new()
    {
        { Jpeg, ImageFormat.Jpeg },
        { Png, ImageFormat.Png },
        { WebP, ImageFormat.WebP },
        { "GIF89a......"u8.ToArray(), null },
        { "%PDF-1.7\n...."u8.ToArray(), null },
        { Encoding.UTF8.GetBytes("Valoare energetica: 389 kcal"), null },
        { "RIFF\0\0\0\0WAVEfmt "u8.ToArray(), null },
        { new byte[] { 0xFF, 0xD8 }, null },
        { Array.Empty<byte>(), null }
    };

    [Fact]
    public void Validate_ValidJpeg_IsValid()
    {
        // Act
        var result = _validator.Validate(new ScanLabelCommand(Guid.NewGuid(), Jpeg));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_TextRenamedAsImage_IsInvalid()
    {
        // Act
        var result = _validator.Validate(new ScanLabelCommand(Guid.NewGuid(), Encoding.UTF8.GetBytes("not an image at all")));

        // Assert
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(ScanErrors.UnsupportedFormat);
    }

    [Fact]
    public void Validate_Empty_IsInvalid()
    {
        // Act
        var result = _validator.Validate(new ScanLabelCommand(Guid.NewGuid(), []));

        // Assert
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(ScanErrors.Empty);
    }

    [Fact]
    public void Validate_OverTenMegabytes_IsInvalid()
    {
        // Arrange
        var image = new byte[ScanLabelCommandValidator.MaxBytes + 1];
        Jpeg.CopyTo(image, 0);

        // Act
        var result = _validator.Validate(new ScanLabelCommand(Guid.NewGuid(), image));

        // Assert
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(ScanErrors.TooLarge);
    }

    [Fact]
    public async Task Handle_Processed_FlagsFieldsBelowThreshold()
    {
        // Arrange
        _ocr.Setup(o => o.ReadLabelAsync(It.IsAny<ReadOnlyMemory<byte>>(), ImageFormat.Png, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Processed(proteinsConfidence: 0.42));

        // Act
        var result = await ScanAsync(Png);

        // Assert
        result.Status.Should().Be(OcrStatus.Processed);
        result.Message.Should().BeNull();
        result.ServingBase.Should().Be(ServingBase.Per100g);
        result.OverallConfidence.Should().Be(0.86);
        result.Fields.Calories.Should().Be(new ScanFieldDto(389, 0.95, NeedsReview: false));
        result.Fields.Proteins.Should().Be(new ScanFieldDto(16.9, 0.42, NeedsReview: true));
        result.Fields.Fats!.NeedsReview.Should().BeFalse("the threshold itself is trusted");
        result.Fields.Fiber.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Unavailable_ReturnsEmptyFormWithMessage()
    {
        // Arrange
        _ocr.Setup(o => o.ReadLabelAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<ImageFormat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OcrResult.Unavailable);

        // Act
        var result = await ScanAsync(Jpeg);

        // Assert
        result.Status.Should().Be(OcrStatus.Unavailable);
        result.Message.Should().Be(ScanErrors.OcrUnavailable);
        result.Fields.Should().Be(ScanFieldsDto.Empty);
        result.ServingBase.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PassesTheImageAndItsDetectedFormat()
    {
        // Arrange
        _ocr.Setup(o => o.ReadLabelAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<ImageFormat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OcrResult.Unavailable);

        // Act
        await ScanAsync(WebP);

        // Assert
        _ocr.Verify(o => o.ReadLabelAsync(
            It.Is<ReadOnlyMemory<byte>>(m => m.ToArray().SequenceEqual(WebP)), ImageFormat.WebP, It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose() => _ctx.Dispose();
}
