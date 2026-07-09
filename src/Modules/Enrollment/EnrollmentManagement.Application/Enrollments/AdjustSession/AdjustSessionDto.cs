namespace EnrollmentManagement.Application.Enrollments.AdjustSession;

// Đổi từ positional IReadOnlyList<Guid> (thứ tự phải đúng) sang named field —
// tránh footgun "SessionIds[0] là old hay new" ở FE.
public sealed record AdjustSessionInputDto(
    Guid OldWeeklySlotId,
    Guid NewWeeklySlotId);

public sealed record AdjustSessionOutputDto(
    Guid EnrollmentId,
    IReadOnlyList<EnrolledSessionOutputDto> EnrolledSessions);

public sealed record EnrolledSessionOutputDto(
    Guid EnrolledSessionId,
    Guid WeeklySlotId,
    string DayOfWeek,
    string SessionType);