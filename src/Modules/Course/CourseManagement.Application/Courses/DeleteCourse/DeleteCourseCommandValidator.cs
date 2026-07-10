using FluentValidation;

namespace CourseManagement.Application.Courses.DeleteCourse;

public sealed class DeleteCourseCommandValidator : AbstractValidator<DeleteCourseCommand> {
    public DeleteCourseCommandValidator() {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("CourseId is required.");
    }
}
