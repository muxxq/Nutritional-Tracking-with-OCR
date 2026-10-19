using MediatR;
using NutriScan.Application.Auth.Dtos;

namespace NutriScan.Application.Auth.Commands.Register;

public sealed record RegisterCommand(string Email, string Password, string FirstName, string LastName) : IRequest<AuthResult>;
