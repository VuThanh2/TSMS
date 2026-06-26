namespace CourseManagement.Application.Common.Interfaces;

/// Cross-BC contract — Course BC queries Enrollment BC through this interface.
public interface IEnrollmentCourseService {
    /// Returns the current number of students enrolled in the given course.
    Task<int> GetEnrollmentCountAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// Returns all courseIds that the given student has enrolled in.
    Task<IReadOnlyList<Guid>> GetEnrolledCourseIdsAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    /// Returns a map of courseId → grade (null if not yet graded)
    /// for all courses the student is enrolled in.
    Task<IReadOnlyDictionary<Guid, decimal?>> GetGradesByCourseAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}