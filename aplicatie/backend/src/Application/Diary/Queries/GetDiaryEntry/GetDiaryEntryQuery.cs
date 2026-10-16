using MediatR;
using NutriScan.Application.Diary.Dtos;

namespace NutriScan.Application.Diary.Queries.GetDiaryEntry;

public sealed record GetDiaryEntryQuery(Guid UserId, Guid EntryId) : IRequest<DiaryEntryDto>;
