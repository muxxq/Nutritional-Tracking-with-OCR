using MediatR;
using NutriScan.Application.Auth.Dtos;

namespace NutriScan.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResult>;
