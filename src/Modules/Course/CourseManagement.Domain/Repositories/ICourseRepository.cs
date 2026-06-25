using CourseManagement.Domain.Entities;
using CourseManagement.Domain.ValueObjects;

namespace CourseManagement.Domain.Repositories;

public interface ICourseRepository {
    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// Loads Course with its ClassSessions collection eager-loaded.
    /// Required for any operation that mutates ClassSessions.
    Task<Course?> GetByIdWithSessionsAsync(Guid id, CancellationToken cancellationToken = default);

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
}