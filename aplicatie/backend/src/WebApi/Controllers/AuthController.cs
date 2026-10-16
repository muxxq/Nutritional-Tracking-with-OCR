using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using NutriScan.Application.Auth;
using NutriScan.Application.Auth.Commands.Login;
using NutriScan.Application.Auth.Commands.Logout;
using NutriScan.Application.Auth.Commands.Refresh;
using NutriScan.Application.Auth.Commands.Register;
using NutriScan.Application.Auth.Dtos;
using NutriScan.Application.Auth.Queries.GetCurrentUser;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.WebApi.Auth;
using NutriScan.WebApi.Contracts.Auth;

namespace NutriScan.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
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

    /// <summary>Rotates the refresh cookie and issues a new access token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new RefreshCommand(RefreshTokenCookie.Read(Request)), cancellationToken);
            return Ok(SignIn(result));
        }
        catch (UnauthorizedException)
        {
            // Handled here, not by the global handler: it clears response headers, which would drop the cookie deletion.
            RefreshTokenCookie.Delete(Response);
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: AuthErrors.InvalidSession);
        }
    }

    /// <summary>Ends the session on this device. Always succeeds.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(RefreshTokenCookie.Read(Request)), cancellationToken);
        RefreshTokenCookie.Delete(Response);
        return NoContent();
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
