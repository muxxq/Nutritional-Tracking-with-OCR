using FluentValidation;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public const int MinimumAge = 18;
    public const int MaximumAge = 100;

    public UpdateProfileCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);

        // Mifflin–St Jeor was validated on adults; calorie-deficit targets are not appropriate for minors.
        RuleFor(c => c.BirthDate)
            .Must(birthDate =>
            {
                var age = new User { BirthDate = birthDate }.AgeOn(DateOnly.FromDateTime(clock.UtcNow));
                return age is >= MinimumAge and <= MaximumAge;
            })
            .WithMessage($"Vârsta trebuie să fie între {MinimumAge} și {MaximumAge} de ani.");

        RuleFor(c => c.HeightCm).InclusiveBetween(100, 250).WithMessage("Înălțimea trebuie să fie între 100 și 250 cm.");
        RuleFor(c => c.WeightKg).InclusiveBetween(30, 300).WithMessage("Greutatea trebuie să fie între 30 și 300 kg.");

        RuleFor(c => c.Gender).IsInEnum();
        RuleFor(c => c.ActivityLevel).IsInEnum();
        RuleFor(c => c.Goal).IsInEnum();
    }
}
