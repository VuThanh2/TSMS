using FluentValidation;

namespace CourseManagement.Application.ClassSessions.DeleteClassSession;

public sealed class DeleteClassSessionCommandValidator : AbstractValidator<DeleteClassSessionCommand> {
    public DeleteClassSessionCommandValidator() {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("CourseId is required.");

        RuleFor(x => x.ClassSessionId)
            .NotEmpty().WithMessage("ClassSessionId is required.");
    }
}