namespace EnrollmentManagement.Application.Schedules.GetStudentSchedule;

public sealed record GetStudentScheduleOutputDto(
    Guid CourseId,
    string CourseName,
    Guid EnrollmentId,
    Guid ClassSessionId,
    DateOnly SessionDate,
    string DayOfWeek,
    string SessionType,
    bool IsCancelled,
    string AttendanceStatus);