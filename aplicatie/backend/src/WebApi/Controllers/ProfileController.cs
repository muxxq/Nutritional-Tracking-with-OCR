using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriScan.Application.Profile.Commands.UpdateProfile;
using NutriScan.Application.Profile.Commands.UpdateTargets;
using NutriScan.Application.Profile.Dtos;
using NutriScan.Application.Profile.Queries.GetProfile;
using NutriScan.WebApi.Auth;
using NutriScan.WebApi.Contracts.Profile;

namespace NutriScan.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController(ISender sender) : ControllerBase
{
    /// <summary>Returns the signed-in user's profile and daily targets.</summary>
    [HttpGet]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new GetProfileQuery(userId), cancellationToken))
            : Unauthorized();

    /// <summary>Replaces the physical profile; calculated targets follow it.</summary>
    [HttpPut]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var command = new UpdateProfileCommand(userId, request.FirstName, request.LastName, request.BirthDate,
            request.HeightCm, request.WeightKg, request.Gender, request.ActivityLevel, request.Goal);
        return Ok(await sender.Send(command, cancellationToken));
    }

    /// <summary>Sets or clears manual target overrides.</summary>
    [HttpPut("targets")]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTargets(UpdateTargetsRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var command = new UpdateTargetsCommand(userId, request.Calories, request.ProteinsGrams,
            request.CarbsGrams, request.FatsGrams);
        return Ok(await sender.Send(command, cancellationToken));
    }
}
