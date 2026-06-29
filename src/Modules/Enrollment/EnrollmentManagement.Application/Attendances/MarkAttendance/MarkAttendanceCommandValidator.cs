using FluentValidation;

namespace EnrollmentManagement.Application.Attendances.MarkAttendance;

public sealed class MarkAttendanceCommandValidator : AbstractValidator<MarkAttendanceCommand> {
    private static readonly string[] ValidStatuses = ["Present", "Absent", "Excused"];

    public MarkAttendanceCommandValidator() {
        RuleFor(x => x.AttendanceId)
            .NotEmpty().WithMessage("AttendanceId is required.");

        RuleFor(x => x.LecturerId)
            .NotEmpty().WithMessage("LecturerId is required.");

        RuleFor(x => x.AttendanceStatus)
            .NotEmpty().WithMessage("AttendanceStatus is required.")
            .Must(s => ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"AttendanceStatus must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}