using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriScan.Application.Statistics.Dtos;
using NutriScan.Application.Statistics.Queries.GetStatistics;
using NutriScan.WebApi.Auth;

namespace NutriScan.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/stats")]
public class StatisticsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Every day of the range (at most 366) with its totals, plus the average and the deviation from the target
    /// over the logged days (FR-08). The client picks the week or month.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<StatisticsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new GetStatisticsQuery(userId, from, to), cancellationToken))
            : Unauthorized();
}
