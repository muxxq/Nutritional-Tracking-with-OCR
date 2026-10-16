using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Application.Foods;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.Diary.Commands.AddDiaryEntry;

public sealed class AddDiaryEntryCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<AddDiaryEntryCommand, DiaryEntryDto>
{
    public async Task<DiaryEntryDto> Handle(AddDiaryEntryCommand request, CancellationToken cancellationToken)
    {
        // Only foods the user can see in the catalog can be logged: global or own, not archived.
        var food = await db.LoadVisibleAsync(request.UserId, request.FoodId, cancellationToken);

        var entry = new DiaryEntry { UserId = request.UserId, FoodItemId = food.Id, CreatedAtUtc = clock.UtcNow };
        entry.Apply(request, food);

        db.DiaryEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        return DiaryEntryDto.From(entry, food);
    }
}
