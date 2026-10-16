using FluentValidation;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Diary.Commands.UpdateDiaryEntry;

public sealed class UpdateDiaryEntryCommandValidator : AbstractValidator<UpdateDiaryEntryCommand>
{
    public UpdateDiaryEntryCommandValidator(IDateTimeProvider clock) => Include(new DiaryEntryFieldsValidator(clock));
}
