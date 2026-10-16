using MediatR;

namespace NutriScan.Application.Auth.Commands.Logout;

public sealed record LogoutCommand(string? RefreshToken) : IRequest;
