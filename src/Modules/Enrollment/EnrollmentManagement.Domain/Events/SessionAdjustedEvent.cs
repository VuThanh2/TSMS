using SharedKernel.Abstractions;

namespace EnrollmentManagement.Domain.Events;

// Published khi Student điều chỉnh WeeklySlot trong Enrollment của mình.
// Attendance của các buổi ĐÃ QUA thuộc slot cũ được giữ nguyên (coi như buổi học thêm);
// chỉ các buổi tương lai được đồng bộ lại (xử lý ở Application Layer).
public sealed record SessionAdjustedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid EnrollmentId { get; init; }
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
    public Guid OldWeeklySlotId { get; init; }
    public Guid NewWeeklySlotId { get; init; }

    public static SessionAdjustedEvent Create(
        Guid enrollmentId,
        Guid studentId,
        Guid courseId,
        Guid oldWeeklySlotId,
        Guid newWeeklySlotId) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            EnrollmentId = enrollmentId,
            StudentId = studentId,
            CourseId = courseId,
            OldWeeklySlotId = oldWeeklySlotId,
            NewWeeklySlotId = newWeeklySlotId
        };
}