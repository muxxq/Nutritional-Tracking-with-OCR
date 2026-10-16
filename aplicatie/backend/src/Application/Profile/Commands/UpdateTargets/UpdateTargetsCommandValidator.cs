using FluentValidation;

namespace NutriScan.Application.Profile.Commands.UpdateTargets;

public sealed class UpdateTargetsCommandValidator : AbstractValidator<UpdateTargetsCommand>
{
    public UpdateTargetsCommandValidator()
    {
        RuleFor(c => c.Calories).InclusiveBetween(1000, 6000).When(c => c.Calories is not null);
        RuleFor(c => c.ProteinsGrams).InclusiveBetween(0, 500).When(c => c.ProteinsGrams is not null);
        RuleFor(c => c.CarbsGrams).InclusiveBetween(0, 1000).When(c => c.CarbsGrams is not null);
        RuleFor(c => c.FatsGrams).InclusiveBetween(0, 300).When(c => c.FatsGrams is not null);
    }
}
