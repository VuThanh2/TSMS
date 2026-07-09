using FluentValidation;

namespace CourseManagement.Application.WeeklySlots.AddWeeklySlot;

public sealed class AddWeeklySlotCommandValidator : AbstractValidator<AddWeeklySlotCommand> {
    private static readonly string[] ValidDaysOfWeek =
        ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    public AddWeeklySlotCommandValidator() {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("CourseId is required.");

        RuleFor(x => x.DayOfWeek)
            .NotEmpty().WithMessage("DayOfWeek is required.")
            .Must(v => ValidDaysOfWeek.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage("DayOfWeek must be a valid day name (e.g. 'Monday').");

        RuleFor(x => x.SessionType)
            .NotEmpty().WithMessage("SessionType is required.")
            .Must(v => v is "Morning" or "Afternoon")
            .WithMessage("SessionType must be 'Morning' or 'Afternoon'.");
    }
}