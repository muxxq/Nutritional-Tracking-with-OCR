using MediatR;
using NutriScan.Application.Diary.Dtos;

namespace NutriScan.Application.Diary.Queries.GetRecentFoods;

/// <summary>Foods the user logged lately, newest first, so they can be logged again in one tap.</summary>
public sealed record GetRecentFoodsQuery(Guid UserId, int Limit = 10) : IRequest<IReadOnlyList<RecentFoodDto>>;
