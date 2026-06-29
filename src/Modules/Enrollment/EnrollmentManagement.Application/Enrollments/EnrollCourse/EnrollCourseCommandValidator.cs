using FluentValidation;

namespace EnrollmentManagement.Application.Enrollments.EnrollCourse;

public sealed class EnrollCourseCommandValidator : AbstractValidator<EnrollCourseCommand> {
    public EnrollCourseCommandValidator() {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("StudentId is required.");

        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("CourseId is required.");

        RuleFor(x => x.SessionIds)
            .NotNull().WithMessage("SessionIds is required.")
            .Must(ids => ids.Count == 2)
            .WithMessage("Exactly 2 session IDs must be selected.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Session IDs must be distinct.");
    }
}