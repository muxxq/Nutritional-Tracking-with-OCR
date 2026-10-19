using FluentValidation;

namespace NutriScan.Application.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);

        // NIST SP 800-63B: length only, no composition rules. The upper bound limits hashing cost.
        RuleFor(c => c.Password).NotEmpty().Length(8, 128);

        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
    }
}
