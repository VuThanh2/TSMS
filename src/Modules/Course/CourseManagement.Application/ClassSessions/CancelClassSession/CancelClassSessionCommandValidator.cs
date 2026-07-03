using FluentValidation;

namespace CourseManagement.Application.ClassSessions.CancelClassSession;

public sealed class CancelClassSessionCommandValidator : AbstractValidator<CancelClassSessionCommand> {
    public CancelClassSessionCommandValidator() {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("CourseId is required.");

        RuleFor(x => x.ClassSessionId)
            .NotEmpty().WithMessage("ClassSessionId is required.");
    }
}