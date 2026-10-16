using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Recipes.Commands.ArchiveRecipe;
using NutriScan.Application.Recipes.Commands.CreateRecipe;
using NutriScan.Application.Recipes.Commands.UpdateRecipe;
using NutriScan.Application.Recipes.Dtos;
using NutriScan.Application.Recipes.Queries.GetRecipe;
using NutriScan.Application.Recipes.Queries.SearchRecipes;
using NutriScan.WebApi.Auth;
using NutriScan.WebApi.Contracts.Recipes;

namespace NutriScan.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/recipes")]
public class RecipesController(ISender sender) : ControllerBase
{
    /// <summary>The user's recipes, optionally filtered by name (case- and diacritic-insensitive), with per-serving values.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<RecipeSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new SearchRecipesQuery(userId, q, page, pageSize), cancellationToken))
            : Unauthorized();

    /// <summary>Returns one of the user's recipes with its ingredients, total and per-serving values.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RecipeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new GetRecipeQuery(userId, id), cancellationToken))
            : Unauthorized();

    /// <summary>Saves a private recipe made from foods the user can see.</summary>
    [HttpPost]
    [ProducesResponseType<RecipeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(SaveRecipeRequest r, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var recipe = await sender.Send(new CreateRecipeCommand(userId, r.Name, r.Description, r.Instructions,
            r.ServingsCount, r.PrepTimeMinutes, r.Ingredients), cancellationToken);
        return Created($"/api/recipes/{recipe.Id}", recipe);
    }

    /// <summary>Replaces all fields and the whole ingredient list of the user's recipe.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<RecipeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, SaveRecipeRequest r, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new UpdateRecipeCommand(userId, id, r.Name, r.Description, r.Instructions,
                r.ServingsCount, r.PrepTimeMinutes, r.Ingredients), cancellationToken))
            : Unauthorized();

    /// <summary>Archives the user's recipe; diary entries logged from it stay.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        await sender.Send(new ArchiveRecipeCommand(userId, id), cancellationToken);
        return NoContent();
    }
}
