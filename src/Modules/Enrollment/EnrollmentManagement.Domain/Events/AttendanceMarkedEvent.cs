using EnrollmentManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;

namespace EnrollmentManagement.Domain.Events;

// Published khi Lecturer cập nhật trạng thái điểm danh của một Student trong buổi học.
public sealed record AttendanceMarkedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid AttendanceId { get; init; }
    public Guid StudentId { get; init; }
    public Guid ClassSessionId { get; init; }
    public Guid CourseId { get; init; }
    public AttendanceStatus Status { get; init; }

    public static AttendanceMarkedEvent Create(
        Guid attendanceId,
        Guid studentId,
        Guid classSessionId,
        Guid courseId,
        AttendanceStatus status) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            AttendanceId = attendanceId,
            StudentId = studentId,
            ClassSessionId = classSessionId,
            CourseId = courseId,
            Status = status
        };
}