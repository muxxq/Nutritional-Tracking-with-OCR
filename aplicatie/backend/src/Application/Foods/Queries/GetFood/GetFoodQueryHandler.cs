using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Foods.Dtos;

namespace NutriScan.Application.Foods.Queries.GetFood;

public sealed class GetFoodQueryHandler(IApplicationDbContext db) : IRequestHandler<GetFoodQuery, FoodDto>
{
    public async Task<FoodDto> Handle(GetFoodQuery request, CancellationToken cancellationToken) =>
        FoodDto.From(await db.LoadVisibleAsync(request.UserId, request.FoodId, cancellationToken), request.UserId);
}
