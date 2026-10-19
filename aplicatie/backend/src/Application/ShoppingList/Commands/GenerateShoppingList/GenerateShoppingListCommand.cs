using MediatR;
using NutriScan.Application.Planner;
using NutriScan.Application.ShoppingList.Dtos;

namespace NutriScan.Application.ShoppingList.Commands.GenerateShoppingList;

/// <summary>FR-12 / UC05: builds the shopping list from the meals planned in a range of days.</summary>
public sealed record GenerateShoppingListCommand(Guid UserId, DateOnly From, DateOnly To) : IRequest<ShoppingListDto>, IPlanRange;
