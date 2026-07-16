using CourseManagement.Domain.Entities;
using CourseManagement.Domain.ValueObjects;
using SharedKernel.Primitives;

namespace CourseManagement.Domain.Repositories;

public interface ICourseRepository {
    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// Loads Course with its WeeklySlots + ClassSessions collections eager-loaded.
    /// Required for any operation that mutates WeeklySlots/ClassSessions (AddWeeklySlot,
    /// RemoveWeeklySlot, UpdateInfo khi đổi EndDate)
    Task<Course?> GetByIdWithSessionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// Loads Course với CHỈ WeeklySlots eager-loaded, KHÔNG load ClassSessions.
    /// Dùng cho các Query chỉ đọc WeeklySlot (vd GetWeeklySlotsQuery) — tránh over-fetch
    /// hàng chục ClassSession không cần thiết chỉ để đọc 2-4 WeeklySlot.
    Task<Course?> GetByIdWithWeeklySlotsAsync(Guid id, CancellationToken cancellationToken = default);

    /// Batch fetch by IDs — avoids N+1 when loading multiple courses at once.
    Task<IReadOnlyList<Course>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    /// sort: optional — bỏ trống thì giữ nguyên thứ tự mặc định (CreatedAt giảm dần).
    /// Chỉ sort được cột nằm trong schema `course`; LecturerName/EnrolledCount là dữ liệu
    /// enrich cross-BC sau khi phân trang nên KHÔNG sort được ở tầng này.
    Task<(IReadOnlyList<Entities.Course> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        CourseStatus? status,
        Guid? lecturerId,
        int page,
        int pageSize,
        SortInput? sort = null,
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

    /// Xóa toàn bộ aggregate Course. WeeklySlots + ClassSessions con được DB cascade
    /// (OnDelete Cascade đã cấu hình ở CourseConfiguration).
    void Remove(Course course);

    void AddWeeklySlot(WeeklySlot weeklySlot);

    void AddClassSession(ClassSession classSession);

    /// Dùng khi AddWeeklySlot sinh hàng loạt ClassSession, hoặc khi gia hạn EndDate (UpdateInfo).
    void AddClassSessions(IEnumerable<ClassSession> classSessions);
}