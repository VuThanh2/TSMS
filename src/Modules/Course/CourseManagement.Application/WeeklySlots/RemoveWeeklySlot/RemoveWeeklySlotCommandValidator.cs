using FluentValidation;

namespace CourseManagement.Application.WeeklySlots.RemoveWeeklySlot;

public sealed class RemoveWeeklySlotCommandValidator : AbstractValidator<RemoveWeeklySlotCommand> {
    public RemoveWeeklySlotCommandValidator() {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("CourseId is required.");

        RuleFor(x => x.WeeklySlotId)
            .NotEmpty().WithMessage("WeeklySlotId is required.");
    }
}