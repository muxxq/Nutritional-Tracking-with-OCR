using MediatR;

namespace NutriScan.Application.Diary.Commands.DeleteDiaryEntry;

public sealed record DeleteDiaryEntryCommand(Guid UserId, Guid EntryId) : IRequest;
