using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Diary.Dtos;

namespace NutriScan.Application.Diary.Commands.UpdateDiaryEntry;

public sealed class UpdateDiaryEntryCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<UpdateDiaryEntryCommand, DiaryEntryDto>
{
    public async Task<DiaryEntryDto> Handle(UpdateDiaryEntryCommand request, CancellationToken cancellationToken)
    {
        // An archived food or recipe still backs the entries logged before it was archived, so they stay editable.
        var entry = await db.LoadOwnAsync(request.UserId, request.EntryId, cancellationToken);

        // The kind can't change (ADR-005 D7); a recipe entry is recalculated from the current recipe, even archived.
        if (entry.IsRecipe)
        {
            entry.Apply(request, entry.Recipe!);
        }
        else
        {
            entry.Apply(request, entry.FoodItem!);
        }

        entry.UpdatedAtUtc = clock.UtcNow;
        await db.SaveOrNotFoundAsync(cancellationToken);

        return DiaryEntryDto.From(entry);
    }
}
