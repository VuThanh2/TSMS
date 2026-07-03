using EnrollmentManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;

namespace EnrollmentManagement.Domain.Entities;

public class EnrolledSession : Entity {
    public Guid EnrollmentId { get; private set; }

    // FK trỏ sang WeeklySlot của CourseManagement BC (cross-BC reference by Id).
    // Trỏ tới khung giờ LẶP LẠI HÀNG TUẦN, KHÔNG phải 1 ClassSession cụ thể —
    // vì Student đăng ký học cho CẢ KỲ, không phải chỉ 1 buổi.
    public Guid WeeklySlotId { get; private set; }

    public SessionType SessionType { get; private set; }

    // Required by EF Core.
    private EnrolledSession() { }

    internal static EnrolledSession Create(
        Guid enrollmentId,
        Guid weeklySlotId,
        SessionType sessionType) {
        return new EnrolledSession {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            WeeklySlotId = weeklySlotId,
            SessionType = sessionType
        };
    }

    // Cập nhật khi Student điều chỉnh ca học (AdjustSession).
    internal void Adjust(Guid newWeeklySlotId, SessionType newSessionType) {
        WeeklySlotId = newWeeklySlotId;
        SessionType = newSessionType;
    }
}