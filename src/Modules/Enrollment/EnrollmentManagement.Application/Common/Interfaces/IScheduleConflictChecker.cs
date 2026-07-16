namespace EnrollmentManagement.Application.Common.Interfaces;

// Domain service (Application Layer): kiểm tra 1 Student có bị trùng lịch học giữa các Course
// khác nhau hay không. Trùng lịch = khoảng ngày GIAO NHAU **VÀ** cùng (DayOfWeek, SessionType).
//
// Dùng DayOfWeek thay vì SessionDate cụ thể vì Enrollment áp dụng cho lịch LẶP LẠI HÀNG TUẦN
// suốt cả kỳ, không phải 1 ngày. Nhưng chỉ so DayOfWeek là CHƯA ĐỦ: 2 Course cùng "Thứ Hai Sáng"
// mà chạy 2 kỳ khác nhau (một cái xong tháng 3, cái kia bắt đầu tháng 4) thì không hề đụng nhau —
// nên phải lọc thêm theo khoảng ngày, cùng công thức với check lịch dạy của Lecturer
// (ICourseQueryService.HasLecturerSlotConflictAsync).
public interface IScheduleConflictChecker {
    // candidateSlots: các (DayOfWeek, SessionType) đang được chọn, cần kiểm tra trùng.
    // candidateStartDate/candidateEndDate: khoảng ngày của Course đang xét — chỉ Course khác có
    //   khoảng ngày giao với nó mới tính là đụng.
    // excludeCourseId: Course hiện tại — không tính các Enrollment thuộc chính Course này.
    Task<bool> HasConflictAsync(
        Guid studentId,
        Guid excludeCourseId,
        DateOnly candidateStartDate,
        DateOnly candidateEndDate,
        IReadOnlyList<(DayOfWeek DayOfWeek, string SessionType)> candidateSlots,
        CancellationToken cancellationToken = default);
}