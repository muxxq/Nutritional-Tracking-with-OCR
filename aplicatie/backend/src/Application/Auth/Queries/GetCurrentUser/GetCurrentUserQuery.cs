using MediatR;
using NutriScan.Application.Auth.Dtos;

namespace NutriScan.Application.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<UserDto>;
