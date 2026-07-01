using FluentValidation;

namespace CourseManagement.Application.Courses.ReplaceLecturer;

public sealed class ReplaceLecturerCommandValidator : AbstractValidator<ReplaceLecturerCommand> {
    public ReplaceLecturerCommandValidator() {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("CourseId is required.");

        RuleFor(x => x.NewLecturerId)
            .NotEmpty().WithMessage("NewLecturerId is required.");
    }
}