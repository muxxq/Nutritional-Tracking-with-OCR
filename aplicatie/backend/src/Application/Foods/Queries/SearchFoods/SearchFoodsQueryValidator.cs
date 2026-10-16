using FluentValidation;

namespace NutriScan.Application.Foods.Queries.SearchFoods;

public sealed class SearchFoodsQueryValidator : AbstractValidator<SearchFoodsQuery>
{
    public const int MaxPageSize = 50;

    // Keeps (page − 1) × pageSize far from int overflow, which would become a negative SQL OFFSET.
    public const int MaxPage = 10_000;

    public SearchFoodsQueryValidator()
    {
        RuleFor(q => q.Page).InclusiveBetween(1, MaxPage);
        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(q => q.Scope).IsInEnum();
        RuleFor(q => q.Query).MaximumLength(200);
    }
}
