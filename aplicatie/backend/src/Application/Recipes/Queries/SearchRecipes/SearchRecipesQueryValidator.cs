using FluentValidation;
using NutriScan.Application.Foods.Queries.SearchFoods;

namespace NutriScan.Application.Recipes.Queries.SearchRecipes;

public sealed class SearchRecipesQueryValidator : AbstractValidator<SearchRecipesQuery>
{
    public SearchRecipesQueryValidator()
    {
        RuleFor(q => q.Page).InclusiveBetween(1, SearchFoodsQueryValidator.MaxPage);
        RuleFor(q => q.PageSize).InclusiveBetween(1, SearchFoodsQueryValidator.MaxPageSize);
        RuleFor(q => q.Query).MaximumLength(200);
    }
}
