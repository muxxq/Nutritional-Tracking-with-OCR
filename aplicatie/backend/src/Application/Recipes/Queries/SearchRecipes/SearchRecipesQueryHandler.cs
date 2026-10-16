using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Recipes.Dtos;
using NutriScan.Domain.Common;

namespace NutriScan.Application.Recipes.Queries.SearchRecipes;

/// <summary>
/// The user's recipes that are not archived, filtered like food search (every word, no diacritics), sorted by name.
/// Totals are summed in SQL by projection, so a page never loads its ingredients into memory.
/// </summary>
public sealed class SearchRecipesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<SearchRecipesQuery, PagedResult<RecipeSummaryDto>>
{
    public async Task<PagedResult<RecipeSummaryDto>> Handle(SearchRecipesQuery request, CancellationToken cancellationToken)
    {
        var recipes = db.Recipes.AsNoTracking().Where(r => r.UserId == request.UserId && r.ArchivedAtUtc == null);

        foreach (var term in TextNormalizer.SearchTerms(request.Query))
        {
            recipes = recipes.Where(r => r.SearchText.Contains(term));
        }

        var totalCount = await recipes.CountAsync(cancellationToken);

        var rows = await recipes
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new
            {
                r.Id, r.Name, r.ServingsCount, r.PrepTimeMinutes,
                Count = r.Ingredients.Count,
                Calories = r.Ingredients.Sum(i => i.FoodItem.Calories * i.QuantityGrams / 100),
                Proteins = r.Ingredients.Sum(i => i.FoodItem.Proteins * i.QuantityGrams / 100),
                Carbs = r.Ingredients.Sum(i => i.FoodItem.Carbohydrates * i.QuantityGrams / 100),
                Fats = r.Ingredients.Sum(i => i.FoodItem.Fats * i.QuantityGrams / 100)
            })
            .ToListAsync(cancellationToken);

        // Divided and rounded here, like RecipeNutrition: per serving comes from the unrounded total.
        var items = rows
            .Select(r => new RecipeSummaryDto(
                r.Id, r.Name, r.ServingsCount, r.PrepTimeMinutes, r.Count,
                new NutrientsDto(r.Calories / r.ServingsCount, r.Proteins / r.ServingsCount,
                    r.Carbs / r.ServingsCount, r.Fats / r.ServingsCount).Rounded()))
            .ToList();

        return new PagedResult<RecipeSummaryDto>(items, request.Page, request.PageSize, totalCount);
    }
}
