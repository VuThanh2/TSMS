using FluentValidation;

namespace CourseManagement.Application.ClassSessions.UpdateClassSession;

public sealed class UpdateClassSessionCommandValidator : AbstractValidator<UpdateClassSessionCommand> {
    public UpdateClassSessionCommandValidator() {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("CourseId is required.");

        RuleFor(x => x.ClassSessionId)
            .NotEmpty().WithMessage("ClassSessionId is required.");

        RuleFor(x => x.SessionDate)
            .NotEmpty().WithMessage("Session date is required.");

        RuleFor(x => x.SessionType)
            .NotEmpty().WithMessage("SessionType is required.")
            .Must(v => v is "Morning" or "Afternoon")
            .WithMessage("SessionType must be 'Morning' or 'Afternoon'.");
    }
}