using MediatR;
using NutriScan.Application.Foods.Dtos;

namespace NutriScan.Application.Foods.Queries.GetFood;

public sealed record GetFoodQuery(Guid UserId, Guid FoodId) : IRequest<FoodDto>;
