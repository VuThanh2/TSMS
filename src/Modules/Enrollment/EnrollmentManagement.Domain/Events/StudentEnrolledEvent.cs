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

    public static StudentEnrolledEvent Create(
        Guid enrollmentId,
        Guid studentId,
        Guid courseId,
        DateTime enrolledAt) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            EnrollmentId = enrollmentId,
            StudentId = studentId,
            CourseId = courseId,
            EnrolledAt = enrolledAt
        };
}