using CourseManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;

namespace CourseManagement.Domain.Entities;

// Định nghĩa 1 khung giờ học LẶP LẠI HÀNG TUẦN của Course (vd: Thứ 2 - Sáng).
// ClassSession là các buổi học cụ thể (có ngày thật) được sinh tự động từ WeeklySlot này,
// trải dài suốt DateRange của Course. Tách riêng 2 khái niệm này để "1 lần chọn lịch"
// của Student áp dụng cho CẢ KỲ, không phải chỉ 1 buổi cụ thể.
public class WeeklySlot : Entity {
    public Guid CourseId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public SessionType SessionType { get; private set; }

    // Required by EF Core.
    private WeeklySlot() { }

    // Chỉ được tạo thông qua Course.AddWeeklySlot() — đảm bảo invariant
    // (không trùng DayOfWeek+SessionType, Course chưa Completed) luôn được validate ở aggregate root.
    internal static WeeklySlot Create(Guid courseId, DayOfWeek dayOfWeek, SessionType sessionType) {
        return new WeeklySlot {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            DayOfWeek = dayOfWeek,
            SessionType = sessionType
        };
    }
}