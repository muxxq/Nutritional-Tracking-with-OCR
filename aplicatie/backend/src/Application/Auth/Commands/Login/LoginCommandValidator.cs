using FluentValidation;

namespace NutriScan.Application.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().MaximumLength(256);

        // No minimum length here: rejecting short passwords at login would reveal the password policy.
        RuleFor(c => c.Password).NotEmpty().MaximumLength(128);
    }
}
