using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Diary.Dtos;

namespace NutriScan.Application.Diary.Queries.GetRecentFoods;

public sealed class GetRecentFoodsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRecentFoodsQuery, IReadOnlyList<RecentFoodDto>>
{
    /// <summary>How many of the newest entries are scanned; keeps the query bounded for long-time users.</summary>
    private const int Window = 200;

    public async Task<IReadOnlyList<RecentFoodDto>> Handle(GetRecentFoodsQuery request, CancellationToken cancellationToken)
    {
        var latest = await db.DiaryEntries
            .Include(d => d.FoodItem)
            .Where(d => d.UserId == request.UserId && d.FoodItemId != null && d.FoodItem!.ArchivedAtUtc == null)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Take(Window)
            .ToListAsync(cancellationToken);

        // Grouped in memory: GroupBy + First doesn't translate to SQL reliably. The first entry per food is the newest.
        return latest
            .GroupBy(d => d.FoodItemId)
            .Select(g => g.First())
            .Take(request.Limit)
            .Select(e => new RecentFoodDto(DiaryFoodDto.From(e.FoodItem!), e.QuantityGrams!.Value, e.Servings, e.CreatedAtUtc))
            .ToList();
    }
}
