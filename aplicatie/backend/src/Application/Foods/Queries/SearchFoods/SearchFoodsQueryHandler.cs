using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Foods.Dtos;
using NutriScan.Domain.Common;

namespace NutriScan.Application.Foods.Queries.SearchFoods;

/// <summary>
/// Case- and diacritic-insensitive search: every word of the query must appear in the normalized SearchText.
/// Order: name starts with the first word, then verified foods, then alphabetical (Id keeps paging stable).
/// </summary>
public sealed class SearchFoodsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<SearchFoodsQuery, PagedResult<FoodSummaryDto>>
{
    public async Task<PagedResult<FoodSummaryDto>> Handle(SearchFoodsQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var foods = db.FoodItems.AsNoTracking().Where(f => f.ArchivedAtUtc == null);

        foods = request.Scope == FoodScope.Mine
            ? foods.Where(f => f.CreatedByUserId == userId)
            : foods.Where(f => f.CreatedByUserId == null || f.CreatedByUserId == userId);

        var terms = TextNormalizer.SearchTerms(request.Query);
        foreach (var term in terms)
        {
            foods = foods.Where(f => f.SearchText.Contains(term));
        }

        var firstTerm = terms.Count > 0 ? terms[0] : string.Empty;
        var totalCount = await foods.CountAsync(cancellationToken);

        var items = await foods
            .OrderByDescending(f => firstTerm != string.Empty && f.SearchText.StartsWith(firstTerm))
            .ThenByDescending(f => f.IsVerified)
            .ThenBy(f => f.Name)
            .ThenBy(f => f.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(f => new FoodSummaryDto(
                f.Id, f.Name, f.Brand, f.Calories, f.Proteins, f.Carbohydrates, f.Fats,
                f.IsVerified, f.CreatedByUserId == userId))
            .ToListAsync(cancellationToken);

        return new PagedResult<FoodSummaryDto>(items, request.Page, request.PageSize, totalCount);
    }
}
