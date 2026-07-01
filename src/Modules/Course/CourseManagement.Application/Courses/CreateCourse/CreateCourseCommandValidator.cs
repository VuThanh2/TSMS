using FluentValidation;

namespace CourseManagement.Application.Courses.CreateCourse;

public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand> {
    public CreateCourseCommandValidator() {
        RuleFor(x => x.LecturerId)
            .NotEmpty().WithMessage("LecturerId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Course name is required.")
            .MaximumLength(200).WithMessage("Course name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0).WithMessage("Max capacity must be a positive integer.");
    }
}