using CourseManagement.Domain.ValueObjects;

namespace CourseManagement.Application.Common.Interfaces;

/// Cross-BC read contract — exposes Course data to other Bounded Contexts.
public interface ICourseQueryService {
    /// Returns true if the course exists and is in Upcoming status.
    Task<bool> IsUpcomingAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<CourseStatus?> GetStatusAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<int?> GetMaxCapacityAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// Returns true if the lecturer has any course whose date range overlaps with the given range.
    Task<bool> HasOverlappingCourseAsync(
        Guid lecturerId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeCourseId = null,
        CancellationToken cancellationToken = default);

    /// Returns true if the lecturer has any course in Upcoming or Active status.
    Task<bool> HasActiveCoursesByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);
}