using MediatR;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Foods.Dtos;

namespace NutriScan.Application.Foods.Queries.SearchFoods;

public sealed record SearchFoodsQuery(Guid UserId, string? Query, FoodScope Scope = FoodScope.All, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<FoodSummaryDto>>;
