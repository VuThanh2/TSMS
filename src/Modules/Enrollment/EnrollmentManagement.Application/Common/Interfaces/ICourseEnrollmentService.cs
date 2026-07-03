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

    // Lấy tất cả WeeklySlot của course kèm SessionType — dùng để: (1) validate Student chọn đúng 2 slot,
    // (2) build candidate cho ScheduleConflictChecker, (3) hiển thị DayOfWeek cho response DTO.
    Task<IReadOnlyList<WeeklySlotLookup>> GetWeeklySlotsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);

    // Lấy WeeklySlot của NHIỀU course cùng lúc — dùng cho ScheduleConflictChecker, tránh N+1
    // khi Student đã enroll nhiều Course khác nhau.
    Task<IReadOnlyList<WeeklySlotLookup>> GetWeeklySlotsByCourseIdsAsync(
        IReadOnlyList<Guid> courseIds,
        CancellationToken cancellationToken = default);

    // Lấy tất cả ClassSessions của course kèm SessionType.
    // Dùng cho: enrich TotalSessionsInCourse của StudentEnrolledEvent (đếm TOÀN BỘ course,
    // không lọc theo slot Student chọn).
    Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);

    // Lấy các ClassSessions thuộc đúng những WeeklySlotId được chỉ định.
    // Dùng để pre-populate Attendance CHỈ cho 2 slot Student thực sự chọn (EnrollCourse),
    // và để đồng bộ Attendance khi đổi slot (AdjustSession) — KHÔNG lấy toàn bộ ClassSession của course.
    Task<IReadOnlyList<ClassSessionLookup>> GetClassSessionsByWeeklySlotIdsAsync(
        IReadOnlyList<Guid> weeklySlotIds,
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

public sealed record WeeklySlotLookup(
    Guid WeeklySlotId,
    Guid CourseId,
    string DayOfWeek,
    string SessionType);

public sealed record ClassSessionLookup(
    Guid ClassSessionId,
    Guid CourseId,
    Guid WeeklySlotId,
    DateOnly SessionDate,
    string SessionType);

public sealed record CourseLookup(
    Guid CourseId,
    string CourseName,
    Guid LecturerId);