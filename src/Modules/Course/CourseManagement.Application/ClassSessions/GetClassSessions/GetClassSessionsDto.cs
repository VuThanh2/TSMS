namespace CourseManagement.Application.ClassSessions.GetClassSessions;

public sealed record GetClassSessionsOutputDto(
    Guid ClassSessionId,
    DateOnly SessionDate,
    string DayOfWeek,
    string SessionType,
    bool IsPast);