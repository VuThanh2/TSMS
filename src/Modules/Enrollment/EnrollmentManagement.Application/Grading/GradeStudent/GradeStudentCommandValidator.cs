using FluentValidation;

namespace EnrollmentManagement.Application.Grading.GradeStudent;

public sealed class GradeStudentCommandValidator : AbstractValidator<GradeStudentCommand> {
    public GradeStudentCommandValidator() {
        RuleFor(x => x.EnrollmentId)
            .NotEmpty().WithMessage("EnrollmentId is required.");

        RuleFor(x => x.LecturerId)
            .NotEmpty().WithMessage("LecturerId is required.");

        RuleFor(x => x.Grade)
            .InclusiveBetween(0m, 10m)
            .WithMessage("Grade must be between 0 and 10.");
    }
}