using SharedKernel.Abstractions;

namespace EnrollmentManagement.Domain.Events;

// Published khi Student đăng ký thành công một Course.
public sealed record StudentEnrolledEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid EnrollmentId { get; init; }
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
    public DateTime EnrolledAt { get; init; }
    public string StudentFullName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string CourseName { get; init; } = string.Empty;
    public string CourseStatus { get; init; } = string.Empty;
    public int TotalSessionsInCourse { get; init; }
 
    public static StudentEnrolledEvent Create(
        Guid enrollmentId,
        Guid studentId,
        Guid courseId,
        DateTime enrolledAt,
        string studentFullName,
        string studentEmail,
        string courseName,
        string courseStatus,
        int totalSessionsInCourse) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            EnrollmentId = enrollmentId,
            StudentId = studentId,
            CourseId = courseId,
            EnrolledAt = enrolledAt,
            StudentFullName = studentFullName,
            StudentEmail = studentEmail,
            CourseName = courseName,
            CourseStatus = courseStatus,
            TotalSessionsInCourse = totalSessionsInCourse
        };
}