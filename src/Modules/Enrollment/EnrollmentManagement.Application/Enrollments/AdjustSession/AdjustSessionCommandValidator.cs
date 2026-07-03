using FluentValidation;

namespace EnrollmentManagement.Application.Enrollments.AdjustSession;

public sealed class AdjustSessionCommandValidator : AbstractValidator<AdjustSessionCommand> {
    public AdjustSessionCommandValidator() {
        RuleFor(x => x.EnrollmentId)
            .NotEmpty().WithMessage("EnrollmentId is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("StudentId is required.");

        RuleFor(x => x.OldWeeklySlotId)
            .NotEmpty().WithMessage("OldWeeklySlotId is required.");

        RuleFor(x => x.NewWeeklySlotId)
            .NotEmpty().WithMessage("NewWeeklySlotId is required.")
            .NotEqual(x => x.OldWeeklySlotId)
            .WithMessage("NewWeeklySlotId must differ from OldWeeklySlotId.");
    }
}