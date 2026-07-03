namespace EnrollmentManagement.Application.Common.Interfaces;

// Domain service (Application Layer): kiểm tra 1 Student có bị trùng lịch học
// (cùng DayOfWeek + cùng SessionType) giữa các Course khác nhau hay không.
// Dùng DayOfWeek thay vì SessionDate cụ thể — vì Enrollment áp dụng cho lịch LẶP LẠI HÀNG TUẦN
// suốt cả kỳ, không phải chỉ 1 ngày.
public interface IScheduleConflictChecker {
    // candidateSlots: các (DayOfWeek, SessionType) đang được chọn, cần kiểm tra trùng.
    // excludeCourseId: Course hiện tại — không tính các Enrollment thuộc chính Course này.
    Task<bool> HasConflictAsync(
        Guid studentId,
        Guid excludeCourseId,
        IReadOnlyList<(DayOfWeek DayOfWeek, string SessionType)> candidateSlots,
        CancellationToken cancellationToken = default);
}