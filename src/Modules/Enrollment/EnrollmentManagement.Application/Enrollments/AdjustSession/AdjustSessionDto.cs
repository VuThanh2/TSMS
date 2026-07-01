namespace EnrollmentManagement.Application.Enrollments.AdjustSession;

public sealed record AdjustSessionInputDto(IReadOnlyList<Guid> SessionIds);

public sealed record AdjustSessionOutputDto(
    Guid EnrollmentId,
    IReadOnlyList<EnrolledSessionOutputDto> EnrolledSessions);

public sealed record EnrolledSessionOutputDto(
    Guid EnrolledSessionId,
    Guid ClassSessionId,
    string DayOfWeek,
    string SessionType);