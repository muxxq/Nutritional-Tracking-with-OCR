using MediatR;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Foods.Commands.ArchiveFood;

/// <summary>"Deleting" archives: the food leaves search and the user's list, but diary history keeps it.</summary>
public sealed class ArchiveFoodCommandHandler(IApplicationDbContext db, IDateTimeProvider clock) : IRequestHandler<ArchiveFoodCommand>
{
    public async Task Handle(ArchiveFoodCommand request, CancellationToken cancellationToken)
    {
        var food = await db.LoadEditableAsync(request.UserId, request.FoodId, cancellationToken);
        food.Archive(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }
}
