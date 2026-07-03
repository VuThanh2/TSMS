namespace CourseManagement.Application.ClassSessions.GetClassSessions;

public sealed record GetClassSessionsOutputDto(
    Guid ClassSessionId,
    Guid WeeklySlotId,
    DateOnly SessionDate,
    string DayOfWeek,
    string SessionType,
    bool IsPast,
    bool IsCancelled);