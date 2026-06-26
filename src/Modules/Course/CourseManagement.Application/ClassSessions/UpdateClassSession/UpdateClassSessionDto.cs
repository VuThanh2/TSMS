namespace CourseManagement.Application.ClassSessions.UpdateClassSession;

public sealed record UpdateClassSessionInputDto(
    DateOnly SessionDate,
    string SessionType);

public sealed record UpdateClassSessionOutputDto(
    Guid ClassSessionId,
    Guid CourseId,
    DateOnly SessionDate,
    string DayOfWeek,
    string SessionType,
    bool IsPast);