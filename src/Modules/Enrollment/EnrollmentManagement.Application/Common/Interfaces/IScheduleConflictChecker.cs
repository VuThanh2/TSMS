namespace EnrollmentManagement.Application.Common.Interfaces;

// Domain service (Application Layer): kiểm tra 1 Student có bị trùng lịch học
// (cùng SessionDate + cùng SessionType) giữa các Course khác nhau hay không.
public interface IScheduleConflictChecker {
    // candidateSlots: các (SessionDate, SessionType) đang được chọn, cần kiểm tra trùng.
    // excludeCourseId: Course hiện tại — không tính các Enrollment thuộc chính Course này.
    Task<bool> HasConflictAsync(
        Guid studentId,
        Guid excludeCourseId,
        IReadOnlyList<(DateOnly SessionDate, string SessionType)> candidateSlots,
        CancellationToken cancellationToken = default);
}