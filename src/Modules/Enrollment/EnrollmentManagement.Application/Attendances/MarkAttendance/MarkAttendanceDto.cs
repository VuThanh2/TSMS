namespace EnrollmentManagement.Application.Attendances.MarkAttendance;

public sealed record MarkAttendanceInputDto(string AttendanceStatus);

public sealed record MarkAttendanceOutputDto(
    Guid AttendanceId,
    Guid ClassSessionId,
    Guid StudentId,
    string AttendanceStatus);
