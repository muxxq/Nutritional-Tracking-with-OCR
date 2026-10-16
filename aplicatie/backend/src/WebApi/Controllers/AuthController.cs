using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using NutriScan.Application.Auth.Commands.Login;
using NutriScan.Application.Auth.Commands.Register;
using NutriScan.Application.Auth.Dtos;
using NutriScan.Application.Auth.Queries.GetCurrentUser;
using NutriScan.Application.Common.Interfaces;
using NutriScan.WebApi.Auth;
using NutriScan.WebApi.Contracts.Auth;

namespace NutriScan.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender, ITokenService tokens) : ControllerBase
{
    /// <summary>Creates an account and signs the user in.</summary>
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RegisterCommand(request.Email, request.Password, request.FirstName, request.LastName), cancellationToken);

        return Created("/api/auth/me", SignIn(result));
    }

    /// <summary>Signs in with email and password.</summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return Ok(SignIn(result));
    }

    /// <summary>Returns the signed-in user.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
        {
            return Unauthorized();
        }

        return Ok(await sender.Send(new GetCurrentUserQuery(userId), cancellationToken));
    }

    private AuthResponse SignIn(AuthResult result)
    {
        RefreshTokenCookie.Append(Response, result.RefreshToken, tokens.RefreshTokenLifetime);
        return AuthResponse.From(result);
    }
}
