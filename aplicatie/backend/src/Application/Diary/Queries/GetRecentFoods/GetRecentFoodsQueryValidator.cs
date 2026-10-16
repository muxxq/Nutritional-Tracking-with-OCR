using FluentValidation;

namespace NutriScan.Application.Diary.Queries.GetRecentFoods;

public sealed class GetRecentFoodsQueryValidator : AbstractValidator<GetRecentFoodsQuery>
{
    public GetRecentFoodsQueryValidator() => RuleFor(q => q.Limit).InclusiveBetween(1, 30);
}
