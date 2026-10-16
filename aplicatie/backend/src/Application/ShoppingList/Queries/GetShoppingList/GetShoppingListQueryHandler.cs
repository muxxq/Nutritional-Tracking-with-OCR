using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.ShoppingList.Dtos;

namespace NutriScan.Application.ShoppingList.Queries.GetShoppingList;

public sealed class GetShoppingListQueryHandler(IApplicationDbContext db) : IRequestHandler<GetShoppingListQuery, ShoppingListDto>
{
    public async Task<ShoppingListDto> Handle(GetShoppingListQuery request, CancellationToken cancellationToken) =>
        ShoppingListDto.From(await db.LoadAllAsync(request.UserId, cancellationToken));
}
