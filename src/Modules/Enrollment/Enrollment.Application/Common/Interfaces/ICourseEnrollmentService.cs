namespace Enrollment.Application.Common.Interfaces;

// Cross-BC contract — Enrollment BC queries Course BC qua interface này.
public interface ICourseEnrollmentService {
    // Check course có tồn tại và đang ở trạng thái Upcoming không.
    Task<bool> IsUpcomingAsync(Guid courseId, CancellationToken cancellationToken = default);

    // Lấy MaxCapacity của course — check trước khi enroll.
    Task<int?> GetMaxCapacityAsync(Guid courseId, CancellationToken cancellationToken = default);

    // Lấy tất cả ClassSessions của course kèm SessionType.
    // Dùng để: (1) validate Student chọn đúng 2 session, (2) pre-populate Attendance records.
    Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);
}

public sealed record ClassSessionLookup(
    Guid ClassSessionId,
    Guid CourseId,
    DateOnly SessionDate,
    string SessionType);