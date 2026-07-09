namespace EnrollmentManagement.Application.Schedules.GetLecturerSchedule;

public sealed record GetLecturerScheduleOutputDto(
    Guid CourseId,
    string CourseName,
    Guid ClassSessionId,
    DateOnly SessionDate,
    string DayOfWeek,
    string SessionType,
    bool IsCancelled);