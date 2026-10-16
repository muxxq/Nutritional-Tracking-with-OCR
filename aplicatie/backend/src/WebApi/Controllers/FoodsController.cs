using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Foods;
using NutriScan.Application.Foods.Commands.ArchiveFood;
using NutriScan.Application.Foods.Commands.CreateFood;
using NutriScan.Application.Foods.Commands.UpdateFood;
using NutriScan.Application.Foods.Dtos;
using NutriScan.Application.Foods.Queries.GetFood;
using NutriScan.Application.Foods.Queries.SearchFoods;
using NutriScan.WebApi.Auth;
using NutriScan.WebApi.Contracts.Foods;

namespace NutriScan.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/foods")]
public class FoodsController(ISender sender) : ControllerBase
{
    /// <summary>Searches the global catalog and the user's own foods (case- and diacritic-insensitive).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<FoodSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? query, [FromQuery] FoodScope scope = FoodScope.All,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new SearchFoodsQuery(userId, query, scope, page, pageSize), cancellationToken))
            : Unauthorized();

    /// <summary>Returns a food visible to the user.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<FoodDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new GetFoodQuery(userId, id), cancellationToken))
            : Unauthorized();

    /// <summary>Adds a private food to the user's catalog.</summary>
    [HttpPost]
    [ProducesResponseType<FoodDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(SaveFoodRequest r, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var food = await sender.Send(new CreateFoodCommand(userId, r.Name, r.Brand, r.Barcode, r.ServingSizeGrams,
            r.Calories, r.Proteins, r.Carbohydrates, r.Sugars, r.Fats, r.SaturatedFats, r.Fiber, r.SaltGrams), cancellationToken);
        return Created($"/api/foods/{food.Id}", food);
    }

    /// <summary>Replaces all fields of the user's own food.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<FoodDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, SaveFoodRequest r, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new UpdateFoodCommand(userId, id, r.Name, r.Brand, r.Barcode, r.ServingSizeGrams,
                r.Calories, r.Proteins, r.Carbohydrates, r.Sugars, r.Fats, r.SaturatedFats, r.Fiber, r.SaltGrams), cancellationToken))
            : Unauthorized();

    /// <summary>Archives the user's own food; diary history keeps referring to it.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        await sender.Send(new ArchiveFoodCommand(userId, id), cancellationToken);
        return NoContent();
    }
}
