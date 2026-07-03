using CourseManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace CourseManagement.Domain.Entities;

// 1 buổi học cụ thể (có ngày thật), được sinh tự động từ 1 WeeklySlot khi Admin AddWeeklySlot()
// hoặc khi Course gia hạn EndDate (UpdateInfo()). Không tạo ClassSession rời rạc trực tiếp nữa.
public class ClassSession : Entity {
    public Guid CourseId { get; private set; }
    public Guid WeeklySlotId { get; private set; }
    public DateOnly SessionDate { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public SessionType SessionType { get; private set; }
    
    // Buổi bị hủy vẫn giữ nguyên trong lịch sử, chỉ đánh dấu để không tính điểm danh.
    public bool IsCancelled { get; private set; }

    // Required by EF Core.
    private ClassSession() { }

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
            SessionType = sessionType,
            IsCancelled = false
        };
    }

    public bool IsPast() {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return SessionDate < today;
    }

    // Dời ngày 1 buổi cụ thể (vd nghỉ lễ dời lịch) — không đổi WeeklySlotId gốc.
    // Caller (Course aggregate) phải validate: not past, no duplicate, trong DateRange.
    internal Result Update(DateOnly newSessionDate, SessionType newSessionType) {
        SessionDate = newSessionDate;
        DayOfWeek = newSessionDate.DayOfWeek;
        SessionType = newSessionType;

        return Result.Success();
    }

    // Hủy buổi học (vd nghỉ lễ) — soft state, KHÔNG xóa row. Attendance đã tạo sẵn cho buổi
    // này được giữ nguyên (không bị chạm), Enrollment BC tự lọc IsCancelled ở điểm đọc/ghi
    // (MarkAttendance từ chối cập nhật buổi đã hủy) thông qua ICourseEnrollmentService.
    internal Result Cancel() {
        IsCancelled = true;

        return Result.Success();
    }
}