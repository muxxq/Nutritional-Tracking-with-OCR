using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Application.Planner.Commands.CreatePlannedMeal;
using NutriScan.Application.Planner.Commands.DeletePlannedMeal;
using NutriScan.Application.Planner.Commands.EatPlannedMeal;
using NutriScan.Application.Planner.Commands.UpdatePlannedMeal;
using NutriScan.Application.Planner.Dtos;
using NutriScan.Application.Planner.Queries.GetPlan;
using NutriScan.Application.Planner.Queries.GetPlannedMeal;
using NutriScan.WebApi.Auth;
using NutriScan.WebApi.Contracts.Planner;

namespace NutriScan.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/plan")]
public class PlannerController(ISender sender) : ControllerBase
{
    /// <summary>Every day of the range (at most 31) with its planned meals, totals and status against the calorie target.</summary>
    [HttpGet]
    [ProducesResponseType<PlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPlan([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new GetPlanQuery(userId, from, to), cancellationToken))
            : Unauthorized();

    /// <summary>Returns one of the user's planned meals with its current values.</summary>
    [HttpGet("items/{id:guid}")]
    [ProducesResponseType<PlannedMealDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItem(Guid id, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new GetPlannedMealQuery(userId, id), cancellationToken))
            : Unauthorized();

    /// <summary>Plans a food (grams or servings) or a recipe (servings) for a meal.</summary>
    [HttpPost("items")]
    [ProducesResponseType<PlannedMealDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateItem(CreatePlannedMealRequest r, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var meal = await sender.Send(
            new CreatePlannedMealCommand(userId, r.FoodId, r.RecipeId, r.Date, r.MealType, r.QuantityGrams, r.Servings), cancellationToken);
        return Created($"/api/plan/items/{meal.Id}", meal);
    }

    /// <summary>Changes a planned meal's date, meal and quantity.</summary>
    [HttpPut("items/{id:guid}")]
    [ProducesResponseType<PlannedMealDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(Guid id, UpdatePlannedMealRequest r, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(
                new UpdatePlannedMealCommand(userId, id, r.Date, r.MealType, r.QuantityGrams, r.Servings), cancellationToken))
            : Unauthorized();

    /// <summary>Deletes a planned meal; a diary entry logged from it stays.</summary>
    [HttpDelete("items/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        await sender.Send(new DeletePlannedMealCommand(userId, id), cancellationToken);
        return NoContent();
    }

    /// <summary>"I ate this": logs the planned meal in the diary, on its planned day and meal.</summary>
    [HttpPost("items/{id:guid}/eat")]
    [ProducesResponseType<DiaryEntryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eat(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var entry = await sender.Send(new EatPlannedMealCommand(userId, id), cancellationToken);
        return Created($"/api/diary/entries/{entry.Id}", entry);
    }
}
