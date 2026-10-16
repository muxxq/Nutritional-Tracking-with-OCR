using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriScan.Application.ShoppingList.Commands.AddShoppingListItem;
using NutriScan.Application.ShoppingList.Commands.ClearPurchasedItems;
using NutriScan.Application.ShoppingList.Commands.DeleteShoppingListItem;
using NutriScan.Application.ShoppingList.Commands.GenerateShoppingList;
using NutriScan.Application.ShoppingList.Commands.SetShoppingListItemPurchased;
using NutriScan.Application.ShoppingList.Dtos;
using NutriScan.Application.ShoppingList.Queries.GetShoppingList;
using NutriScan.WebApi.Auth;
using NutriScan.WebApi.Contracts.ShoppingList;

namespace NutriScan.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/shopping-list")]
public class ShoppingListController(ISender sender) : ControllerBase
{
    /// <summary>The user's shopping list, grouped by aisle.</summary>
    [HttpGet]
    [ProducesResponseType<ShoppingListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new GetShoppingListQuery(userId), cancellationToken))
            : Unauthorized();

    /// <summary>Replaces the generated lines with what the meals planned in the range need; manual lines stay.</summary>
    [HttpPost("generate")]
    [ProducesResponseType<ShoppingListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generate(GenerateShoppingListRequest r, CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId
            ? Ok(await sender.Send(new GenerateShoppingListCommand(userId, r.From, r.To), cancellationToken))
            : Unauthorized();

    /// <summary>Adds a line by hand.</summary>
    [HttpPost("items")]
    [ProducesResponseType<ShoppingListItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem(AddShoppingListItemRequest r, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var item = await sender.Send(new AddShoppingListItemCommand(userId, r.Name, r.Quantity, r.Category), cancellationToken);
        return Created($"/api/shopping-list/items/{item.Id}", item);
    }

    /// <summary>Checks or unchecks a line.</summary>
    [HttpPatch("items/{id:guid}")]
    [ProducesResponseType<ShoppingListItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPurchased(Guid id, SetPurchasedRequest r, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        if (r.IsPurchased is not { } isPurchased)
        {
            ModelState.AddModelError(nameof(r.IsPurchased), "Câmpul isPurchased este obligatoriu.");
            return ValidationProblem(ModelState);
        }

        return Ok(await sender.Send(new SetShoppingListItemPurchasedCommand(userId, id, isPurchased), cancellationToken));
    }

    /// <summary>Removes a line.</summary>
    [HttpDelete("items/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        await sender.Send(new DeleteShoppingListItemCommand(userId, id), cancellationToken);
        return NoContent();
    }

    /// <summary>Removes every checked line.</summary>
    [HttpDelete("purchased")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearPurchased(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        await sender.Send(new ClearPurchasedItemsCommand(userId), cancellationToken);
        return NoContent();
    }
}
