namespace EnrollmentManagement.Application.Attendances.GetSessionAttendances;

public sealed record GetSessionAttendancesOutputDto(
    Guid AttendanceId,
    Guid StudentId,
    string? StudentFullName,
    string AttendanceStatus,
    DateTime? MarkedAt);