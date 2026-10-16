using MediatR;
using NutriScan.Application.Profile.Dtos;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.Profile.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    DateOnly BirthDate,
    double HeightCm,
    double WeightKg,
    Gender Gender,
    ActivityLevel ActivityLevel,
    NutritionGoal Goal) : IRequest<ProfileDto>;
