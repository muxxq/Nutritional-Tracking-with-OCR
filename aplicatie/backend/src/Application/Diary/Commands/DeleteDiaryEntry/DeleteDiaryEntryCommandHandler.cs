using MediatR;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Diary.Commands.DeleteDiaryEntry;

public sealed class DeleteDiaryEntryCommandHandler(IApplicationDbContext db) : IRequestHandler<DeleteDiaryEntryCommand>
{
    public async Task Handle(DeleteDiaryEntryCommand request, CancellationToken cancellationToken)
    {
        // A physical delete: nothing references a diary entry, unlike a food.
        var entry = await db.LoadOwnAsync(request.UserId, request.EntryId, cancellationToken);
        db.DiaryEntries.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
    }
}
