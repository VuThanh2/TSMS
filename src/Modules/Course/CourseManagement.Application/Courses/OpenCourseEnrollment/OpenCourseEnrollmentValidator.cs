using FluentValidation;

namespace CourseManagement.Application.Courses.OpenCourseEnrollment;

public sealed class OpenCourseEnrollmentCommandValidator : AbstractValidator<OpenCourseEnrollmentCommand> {
    public OpenCourseEnrollmentCommandValidator() {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("CourseId is required.");
    }
}
