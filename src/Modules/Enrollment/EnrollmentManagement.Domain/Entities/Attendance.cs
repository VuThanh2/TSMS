using EnrollmentManagement.Domain.Events;
using EnrollmentManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Domain.Entities;

// Pre-populated với trạng thái Absent khi Student enroll (Application Layer tạo hàng loạt).
// Lecturer là người duy nhất được phép cập nhật AttendanceStatus.
public class Attendance : AggregateRoot {
    public Guid StudentId { get; private set; }
    public Guid ClassSessionId { get; private set; }

    // Denormalized để query GetSessionAttendances theo CourseId mà không cần JOIN.
    public Guid CourseId { get; private set; }

    public AttendanceStatus Status { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Required by EF Core.
    private Attendance() { }

    // Tạo Attendance record với trạng thái mặc định Absent.
    public static Attendance CreateDefault(
        Guid studentId,
        Guid classSessionId,
        Guid courseId) {
        return new Attendance {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            ClassSessionId = classSessionId,
            CourseId = courseId,
            Status = AttendanceStatus.Absent,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // ── Behaviour methods

    // Cập nhật trạng thái điểm danh.
    public Result Mark(AttendanceStatus newStatus) {
        if (Status == newStatus)
            return Result.Success();
 
        var previousStatus = Status;
 
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
 
        RaiseDomainEvent(AttendanceMarkedEvent.Create(
            Id, StudentId, ClassSessionId, CourseId, previousStatus, newStatus));
 
        return Result.Success();
    }
}