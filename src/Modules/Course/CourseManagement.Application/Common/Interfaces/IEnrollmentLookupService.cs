namespace CourseManagement.Application.Common.Interfaces;

/// Cross-BC contract — Course BC queries Enrollment BC through this interface.
public interface IEnrollmentLookupService {
    /// Returns the current number of students enrolled in the given course.
    Task<int> GetEnrollmentCountAsync(Guid courseId, CancellationToken cancellationToken = default);
}