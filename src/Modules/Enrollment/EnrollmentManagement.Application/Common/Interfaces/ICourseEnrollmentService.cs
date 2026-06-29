namespace EnrollmentManagement.Application.Common.Interfaces;

// Cross-BC contract — Enrollment BC queries Course BC qua interface này.
// Implement bởi CourseManagement.Infrastructure.Services.CourseQueryService.
public interface ICourseEnrollmentService {
    // Check course có tồn tại và đang ở trạng thái Upcoming không.
    Task<bool> IsUpcomingAsync(Guid courseId, CancellationToken cancellationToken = default);

    // Lấy status của course — dùng để validate GradeStudent (Active/Completed) và AdjustSession (not Completed).
    Task<string?> GetStatusAsync(Guid courseId, CancellationToken cancellationToken = default);

    // Lấy MaxCapacity của course — check trước khi enroll.
    Task<int?> GetMaxCapacityAsync(Guid courseId, CancellationToken cancellationToken = default);

    // Lấy tất cả ClassSessions của course kèm SessionType.
    // Dùng để: (1) validate Student chọn đúng 2 session, (2) pre-populate Attendance records.
    Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);

    // Lấy CourseLookup cho nhiều courseIds — dùng cho enrich response DTO.
    Task<IReadOnlyList<CourseLookup>> GetCoursesByIdsAsync(
        IReadOnlyList<Guid> courseIds,
        CancellationToken cancellationToken = default);

    // Lấy toàn bộ Course của một Lecturer — dùng cho GetLecturerSchedule.
    Task<IReadOnlyList<CourseLookup>> GetCoursesByLecturerAsync(
        Guid lecturerId,
        CancellationToken cancellationToken = default);

    // Lấy tất cả ClassSessions của nhiều courses — dùng cho Lecturer Schedule query.
    Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsByCourseIdsAsync(
        IReadOnlyList<Guid> courseIds,
        CancellationToken cancellationToken = default);
}

public sealed record ClassSessionLookup(
    Guid ClassSessionId,
    Guid CourseId,
    DateOnly SessionDate,
    string SessionType);

public sealed record CourseLookup(
    Guid CourseId,
    string CourseName,
    Guid LecturerId);