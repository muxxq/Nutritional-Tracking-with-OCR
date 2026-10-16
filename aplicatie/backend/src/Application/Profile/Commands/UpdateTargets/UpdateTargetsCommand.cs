using MediatR;
using NutriScan.Application.Profile.Dtos;

namespace NutriScan.Application.Profile.Commands.UpdateTargets;

/// <summary>Manual target overrides; null means "calculate from the profile".</summary>
public sealed record UpdateTargetsCommand(
    Guid UserId,
    int? Calories,
    double? ProteinsGrams,
    double? CarbsGrams,
    double? FatsGrams) : IRequest<ProfileDto>;
