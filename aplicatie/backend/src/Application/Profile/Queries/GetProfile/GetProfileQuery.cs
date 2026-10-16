using MediatR;
using NutriScan.Application.Profile.Dtos;

namespace NutriScan.Application.Profile.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IRequest<ProfileDto>;
