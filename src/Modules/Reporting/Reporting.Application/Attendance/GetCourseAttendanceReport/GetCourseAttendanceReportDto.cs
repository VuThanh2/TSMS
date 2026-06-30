namespace Reporting.Application.Attendance.GetCourseAttendanceReport;

public sealed record GetCourseAttendanceReportDto(
    Guid CourseId,
    string CourseName,
    IReadOnlyList<CourseAttendanceItemDto> Items);

public sealed record CourseAttendanceItemDto(
    Guid EnrollmentId,
    Guid StudentId,
    string StudentFullName,
    string StudentEmail,
    int TotalSessions,
    int PresentCount,
    int ExcusedCount,
    int AbsentCount,
    decimal AttendanceRate);