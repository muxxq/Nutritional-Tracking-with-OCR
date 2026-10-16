using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriScan.Application.Diary.Commands.AddDiaryEntry;
using NutriScan.Application.Diary.Commands.DeleteDiaryEntry;
using NutriScan.Application.Diary.Commands.UpdateDiaryEntry;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Application.Diary.Queries.GetDiaryDay;
using NutriScan.Application.Diary.Queries.GetDiaryEntry;
using NutriScan.Application.Diary.Queries.GetRecentFoods;
using NutriScan.WebApi.Auth;
using NutriScan.WebApi.Contracts.Diary;

namespace NutriScan.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/diary")]
public class DiaryController(ISender sender) : ControllerBase
{
    /// <summary>The day's four meals with their entries, totals and the user's targets.</summary>
    [HttpGet("days/{date}")]
    [ProducesResponseType<DiaryDayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDay(DateOnly date, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new GetDiaryDayQuery(userId, date), cancellationToken))
            : Unauthorized();

    /// <summary>Returns one of the user's diary entries.</summary>
    [HttpGet("entries/{id:guid}")]
    [ProducesResponseType<DiaryEntryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEntry(Guid id, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new GetDiaryEntryQuery(userId, id), cancellationToken))
            : Unauthorized();

    /// <summary>Logs a food (grams or servings) or a recipe (servings) at a meal, with a snapshot of its nutrients.</summary>
    [HttpPost("entries")]
    [ProducesResponseType<DiaryEntryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddEntry(AddDiaryEntryRequest r, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var entry = await sender.Send(
            new AddDiaryEntryCommand(userId, r.FoodId, r.RecipeId, r.Date, r.MealType, r.QuantityGrams, r.Servings), cancellationToken);
        return Created($"/api/diary/entries/{entry.Id}", entry);
    }

    /// <summary>Changes an entry's quantity, meal and date; nutrients are recalculated from the food's or recipe's current values.</summary>
    [HttpPut("entries/{id:guid}")]
    [ProducesResponseType<DiaryEntryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEntry(Guid id, UpdateDiaryEntryRequest r, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(
                new UpdateDiaryEntryCommand(userId, id, r.Date, r.MealType, r.QuantityGrams, r.Servings), cancellationToken))
            : Unauthorized();

    /// <summary>Deletes one of the user's diary entries.</summary>
    [HttpDelete("entries/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntry(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        await sender.Send(new DeleteDiaryEntryCommand(userId, id), cancellationToken);
        return NoContent();
    }

    /// <summary>Foods the user logged lately, newest first, with the last quantity used.</summary>
    [HttpGet("recent-foods")]
    [ProducesResponseType<IReadOnlyList<RecentFoodDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRecentFoods([FromQuery] int limit = 10, CancellationToken cancellationToken = default) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new GetRecentFoodsQuery(userId, limit), cancellationToken))
            : Unauthorized();
}
