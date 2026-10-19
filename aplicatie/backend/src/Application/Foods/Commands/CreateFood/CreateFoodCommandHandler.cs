using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Foods.Dtos;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.Foods.Commands.CreateFood;

public sealed class CreateFoodCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateFoodCommand, FoodDto>
{
    public async Task<FoodDto> Handle(CreateFoodCommand request, CancellationToken cancellationToken)
    {
        // User-created foods are private and unverified.
        var food = new FoodItem { CreatedByUserId = request.UserId, IsVerified = false };
        food.Apply(request);

        db.FoodItems.Add(food);
        await db.SaveChangesAsync(cancellationToken);

        return FoodDto.From(food, request.UserId);
    }
}
