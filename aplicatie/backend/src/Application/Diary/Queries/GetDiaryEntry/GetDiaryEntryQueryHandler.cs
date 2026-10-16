using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Diary.Dtos;

namespace NutriScan.Application.Diary.Queries.GetDiaryEntry;

public sealed class GetDiaryEntryQueryHandler(IApplicationDbContext db) : IRequestHandler<GetDiaryEntryQuery, DiaryEntryDto>
{
    public async Task<DiaryEntryDto> Handle(GetDiaryEntryQuery request, CancellationToken cancellationToken)
    {
        var entry = await db.LoadOwnAsync(request.UserId, request.EntryId, cancellationToken);
        return DiaryEntryDto.From(entry, entry.FoodItem);
    }
}
