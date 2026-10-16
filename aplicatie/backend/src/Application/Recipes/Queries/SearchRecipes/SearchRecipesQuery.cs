using MediatR;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Recipes.Dtos;

namespace NutriScan.Application.Recipes.Queries.SearchRecipes;

public sealed record SearchRecipesQuery(Guid UserId, string? Query, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<RecipeSummaryDto>>;
