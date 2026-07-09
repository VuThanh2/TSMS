namespace CourseManagement.Application.Common.Interfaces;

/// Cross-BC contract — Course BC queries Enrollment BC through this interface.
public interface IEnrollmentCourseService {
    /// Returns the current number of students enrolled in the given course.
    Task<int> GetEnrollmentCountAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// Returns all courseIds that the given student has enrolled in.
    Task<IReadOnlyList<Guid>> GetEnrolledCourseIdsAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// Returns true if any active Enrollment currently references this WeeklySlotId.
    /// Dùng làm precondition trước khi RemoveWeeklySlot — không cho xóa slot đang có Student học.
    Task<bool> IsWeeklySlotInUseAsync(Guid weeklySlotId, CancellationToken cancellationToken = default);
}