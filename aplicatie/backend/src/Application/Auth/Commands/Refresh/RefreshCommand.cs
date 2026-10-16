using MediatR;
using NutriScan.Application.Auth.Dtos;

namespace NutriScan.Application.Auth.Commands.Refresh;

public sealed record RefreshCommand(string? RefreshToken) : IRequest<AuthResult>;
