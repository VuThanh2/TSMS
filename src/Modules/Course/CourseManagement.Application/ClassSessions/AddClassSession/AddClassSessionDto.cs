namespace CourseManagement.Application.ClassSessions.AddClassSession;

public sealed record AddClassSessionInputDto(
    DateOnly SessionDate,
    string SessionType);

public sealed record AddClassSessionOutputDto(
    Guid ClassSessionId,
    Guid CourseId,
    DateOnly SessionDate,
    string DayOfWeek,
    string SessionType,
    bool IsPast);