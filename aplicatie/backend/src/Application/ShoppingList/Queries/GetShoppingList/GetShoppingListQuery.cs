using MediatR;
using NutriScan.Application.ShoppingList.Dtos;

namespace NutriScan.Application.ShoppingList.Queries.GetShoppingList;

public sealed record GetShoppingListQuery(Guid UserId) : IRequest<ShoppingListDto>;
