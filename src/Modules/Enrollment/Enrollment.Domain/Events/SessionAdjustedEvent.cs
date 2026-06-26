using SharedKernel.Abstractions;

namespace Enrollment.Domain.Events;

// Published khi Student điều chỉnh một ca học trong Enrollment của mình.
// Attendance record của ca cũ được giữ nguyên (coi như buổi học thêm).
public sealed record SessionAdjustedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid EnrollmentId { get; init; }
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
    public Guid OldClassSessionId { get; init; }
    public Guid NewClassSessionId { get; init; }

    public static SessionAdjustedEvent Create(
        Guid enrollmentId,
        Guid studentId,
        Guid courseId,
        Guid oldClassSessionId,
        Guid newClassSessionId) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            EnrollmentId = enrollmentId,
            StudentId = studentId,
            CourseId = courseId,
            OldClassSessionId = oldClassSessionId,
            NewClassSessionId = newClassSessionId
        };
}