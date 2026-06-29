using FluentValidation;

namespace EnrollmentManagement.Application.Enrollments.AdjustSession;

public sealed class AdjustSessionCommandValidator : AbstractValidator<AdjustSessionCommand> {
    public AdjustSessionCommandValidator() {
        RuleFor(x => x.EnrollmentId)
            .NotEmpty().WithMessage("EnrollmentId is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("StudentId is required.");

        RuleFor(x => x.OldSessionId)
            .NotEmpty().WithMessage("OldSessionId is required.");

        RuleFor(x => x.NewSessionId)
            .NotEmpty().WithMessage("NewSessionId is required.")
            .NotEqual(x => x.OldSessionId)
            .WithMessage("NewSessionId must differ from OldSessionId.");
    }
}