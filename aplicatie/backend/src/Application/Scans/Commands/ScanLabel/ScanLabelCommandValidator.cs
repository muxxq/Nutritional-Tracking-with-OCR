using FluentValidation;

namespace NutriScan.Application.Scans.Commands.ScanLabel;

public sealed class ScanLabelCommandValidator : AbstractValidator<ScanLabelCommand>
{
    /// <summary>10 MB, as in the OCR contract (NFR-09). The controller answers 413 before this; this is the second line.</summary>
    public const int MaxBytes = 10 * 1024 * 1024;

    public ScanLabelCommandValidator()
    {
        RuleFor(c => c.Image)
            .Cascade(CascadeMode.Stop)
            .Must(i => i.Length > 0).WithMessage(ScanErrors.Empty)
            .Must(i => i.Length <= MaxBytes).WithMessage(ScanErrors.TooLarge)
            .Must(i => ImageFormatDetector.Detect(i) is not null).WithMessage(ScanErrors.UnsupportedFormat);
    }
}
