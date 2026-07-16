using CourseManagement.Domain.ValueObjects;

namespace CourseManagement.Application.Common.Interfaces;

/// Cross-BC read contract — exposes Course data to other Bounded Contexts.
public interface ICourseQueryService {
    /// Returns true if the course exists and is in Upcoming status.
    Task<bool> IsUpcomingAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<CourseStatus?> GetStatusAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<int?> GetMaxCapacityAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// Trùng lịch dạy của Lecturer = khoảng ngày GIAO NHAU **VÀ** cùng (DayOfWeek, SessionType).
    /// Thiếu vế slot thì chặn nhầm: 1 Lecturer dạy 2 lớp cùng kỳ nhưng khác ca là hợp lệ.
    /// Thiếu vế ngày cũng chặn nhầm: 2 lớp khác kỳ cùng "Thứ Hai Sáng" không hề đụng nhau.
    /// Course Completed không tính (đã dạy xong).
    Task<bool> HasLecturerSlotConflictAsync(
        Guid lecturerId,
        IReadOnlyList<(DayOfWeek DayOfWeek, SessionType SessionType)> candidateSlots,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeCourseId = null,
        CancellationToken cancellationToken = default);

    /// Returns true if the lecturer has any course in Upcoming or Active status.
    Task<bool> HasActiveCoursesByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);
}