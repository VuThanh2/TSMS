namespace EnrollmentManagement.Application.Attendances.GetCourseAttendanceSummary;

public sealed record GetCourseAttendanceSummaryOutputDto(
    Guid ClassSessionId,
    int PresentCount,
    int ExcusedCount,
    int AbsentCount,
    int TotalCount,
    bool IsMarked);
