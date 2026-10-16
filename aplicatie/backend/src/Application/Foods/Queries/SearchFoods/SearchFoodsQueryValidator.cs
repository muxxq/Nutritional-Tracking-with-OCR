using FluentValidation;

namespace NutriScan.Application.Foods.Queries.SearchFoods;

public sealed class SearchFoodsQueryValidator : AbstractValidator<SearchFoodsQuery>
{
    public const int MaxPageSize = 50;

    public SearchFoodsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(q => q.Scope).IsInEnum();
        RuleFor(q => q.Query).MaximumLength(200);
    }
}
