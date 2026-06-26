using FluentValidation;

namespace CourseManagement.Application.ClassSessions.AddClassSession;

public sealed class AddClassSessionCommandValidator : AbstractValidator<AddClassSessionCommand> {
    public AddClassSessionCommandValidator() {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("CourseId is required.");

        RuleFor(x => x.SessionDate)
            .NotEmpty().WithMessage("Session date is required.");

        RuleFor(x => x.SessionType)
            .NotEmpty().WithMessage("SessionType is required.")
            .Must(v => v is "Morning" or "Afternoon")
            .WithMessage("SessionType must be 'Morning' or 'Afternoon'.");
    }
}