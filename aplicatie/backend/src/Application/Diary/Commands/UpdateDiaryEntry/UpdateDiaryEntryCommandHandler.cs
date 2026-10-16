using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Diary.Dtos;

namespace NutriScan.Application.Diary.Commands.UpdateDiaryEntry;

public sealed class UpdateDiaryEntryCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<UpdateDiaryEntryCommand, DiaryEntryDto>
{
    public async Task<DiaryEntryDto> Handle(UpdateDiaryEntryCommand request, CancellationToken cancellationToken)
    {
        // An archived food still backs the entries logged before it was archived, so they stay editable.
        var entry = await db.LoadOwnAsync(request.UserId, request.EntryId, cancellationToken);

        entry.Apply(request, entry.FoodItem!);
        entry.UpdatedAtUtc = clock.UtcNow;
        await db.SaveOrNotFoundAsync(cancellationToken);

        return DiaryEntryDto.From(entry, entry.FoodItem!);
    }
}
