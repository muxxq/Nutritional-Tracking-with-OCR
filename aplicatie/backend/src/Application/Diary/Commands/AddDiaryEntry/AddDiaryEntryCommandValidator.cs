using FluentValidation;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Diary.Commands.AddDiaryEntry;

public sealed class AddDiaryEntryCommandValidator : AbstractValidator<AddDiaryEntryCommand>
{
    public AddDiaryEntryCommandValidator(IDateTimeProvider clock) => Include(new DiaryEntryFieldsValidator(clock));
}
