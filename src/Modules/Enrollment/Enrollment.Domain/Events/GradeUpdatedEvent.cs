using SharedKernel.Abstractions;

namespace Enrollment.Domain.Events;

// Published khi Lecturer cập nhật điểm đã có của một Enrollment.
// Consumed by:
//   - Reporting BC (cập nhật lại StudentGradeReportView, CourseScoreDistributionView)
//   - SignalR: thông báo real-time đến Student đang online.
public sealed record GradeUpdatedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid EnrollmentId { get; init; }
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
    public decimal PreviousGrade { get; init; }
    public decimal NewGrade { get; init; }

    public static GradeUpdatedEvent Create(
        Guid enrollmentId,
        Guid studentId,
        Guid courseId,
        decimal previousGrade,
        decimal newGrade) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            EnrollmentId = enrollmentId,
            StudentId = studentId,
            CourseId = courseId,
            PreviousGrade = previousGrade,
            NewGrade = newGrade
        };
}