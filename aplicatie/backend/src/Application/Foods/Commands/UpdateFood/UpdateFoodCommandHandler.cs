using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Foods.Dtos;

namespace NutriScan.Application.Foods.Commands.UpdateFood;

public sealed class UpdateFoodCommandHandler(IApplicationDbContext db) : IRequestHandler<UpdateFoodCommand, FoodDto>
{
    public async Task<FoodDto> Handle(UpdateFoodCommand request, CancellationToken cancellationToken)
    {
        var food = await db.LoadEditableAsync(request.UserId, request.FoodId, cancellationToken);
        food.Apply(request);
        await db.SaveChangesAsync(cancellationToken);

        return FoodDto.From(food, request.UserId);
    }
}
