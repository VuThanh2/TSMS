using CourseManagement.Domain.Entities;
using CourseManagement.Domain.ValueObjects;

namespace CourseManagement.Domain.Repositories;

public interface ICourseRepository {
    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// Loads Course with its WeeklySlots + ClassSessions collections eager-loaded.
    /// Required for any operation that mutates WeeklySlots/ClassSessions.
    Task<Course?> GetByIdWithSessionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// Batch fetch by IDs — avoids N+1 when loading multiple courses at once.
    Task<IReadOnlyList<Course>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Entities.Course> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        CourseStatus? status,
        Guid? lecturerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// Returns courses assigned to a specific lecturer, optionally filtered by status.
    Task<IReadOnlyList<Course>> GetByLecturerIdAsync(
        Guid lecturerId,
        CourseStatus? status = null,
        CancellationToken cancellationToken = default);

    /// Returns all courses in Upcoming or Active status, used by the background job.
    Task<IReadOnlyList<Course>> GetActiveTransitionCandidatesAsync(
        CancellationToken cancellationToken = default);

    void Add(Course course);
    void Update(Course course);

    void AddWeeklySlot(WeeklySlot weeklySlot);

    void AddClassSession(ClassSession classSession);

    /// Dùng khi AddWeeklySlot sinh hàng loạt ClassSession, hoặc khi gia hạn EndDate (UpdateInfo).
    void AddClassSessions(IEnumerable<ClassSession> classSessions);

    /// Dùng khi RemoveWeeklySlot hoặc rút ngắn EndDate — dọn các ClassSession tương lai bị hủy.
    void RemoveClassSessions(IEnumerable<ClassSession> classSessions);
}