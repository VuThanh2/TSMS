using SharedKernel.Abstractions;

namespace Enrollment.Domain.Events;

// Published khi Lecturer chấm điểm lần đầu cho một Enrollment.
// Consumed by:
//   - Reporting BC (cập nhật StudentGradeReportView, CourseScoreDistributionView)
//   - SignalR: thông báo real-time đến Student đang online.
public sealed record GradeAssignedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid EnrollmentId { get; init; }
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
    public decimal Grade { get; init; }

    public static GradeAssignedEvent Create(
        Guid enrollmentId,
        Guid studentId,
        Guid courseId,
        decimal grade) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            EnrollmentId = enrollmentId,
            StudentId = studentId,
            CourseId = courseId,
            Grade = grade
        };
}