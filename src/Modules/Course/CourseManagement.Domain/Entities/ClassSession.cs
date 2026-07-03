using CourseManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace CourseManagement.Domain.Entities;

// 1 buổi học cụ thể (có SessionDate thật), được sinh tự động từ 1 WeeklySlot
// Không còn tạo ClassSession rời rạc trực tiếp — luôn phải thông qua WeeklySlot
// để đảm bảo invariant "Student chọn 1 slot = áp dụng cho mọi buổi cùng slot đó".
public class ClassSession : Entity {
    public Guid CourseId { get; private set; }

    // FK tới WeeklySlot sinh ra buổi học này — dùng để nhóm ClassSession theo lịch lặp lại hàng tuần.
    public Guid WeeklySlotId { get; private set; }

    public DateOnly SessionDate { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public SessionType SessionType { get; private set; }

    // Required by EF Core.
    private ClassSession() { }

    // Chỉ gọi nội bộ từ Course aggregate (qua AddWeeklySlot/regenerate) — không public
    // để tránh tạo ClassSession không gắn với WeeklySlot nào.
    internal static ClassSession Create(
        Guid courseId,
        Guid weeklySlotId,
        DateOnly sessionDate,
        SessionType sessionType) {
        return new ClassSession {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            WeeklySlotId = weeklySlotId,
            SessionDate = sessionDate,
            DayOfWeek = sessionDate.DayOfWeek,
            SessionType = sessionType
        };
    }

    /// Returns true if this session has already passed relative to today.
    /// Convention: today's session is NOT considered past (date-only comparison).
    public bool IsPast() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return SessionDate < today;
    }

    // Dời ngày 1 buổi CỤ THỂ (vd nghỉ lễ dời lịch) — không đổi WeeklySlotId gốc,
    // vẫn thuộc cùng 1 khung giờ lặp lại hàng tuần.
    // Caller (Course aggregate) phải validate: not past, no duplicate, trong DateRange.
    internal Result Update(DateOnly newSessionDate, SessionType newSessionType) {
        SessionDate = newSessionDate;
        DayOfWeek = newSessionDate.DayOfWeek;
        SessionType = newSessionType;

        return Result.Success();
    }
}