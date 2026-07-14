namespace CourseManagement.Application.Common.Interfaces;

// Cross-BC contract — Course BC nhờ Enrollment BC đồng bộ Attendance sau khi lịch học của
// Course thay đổi làm SINH THÊM ClassSession (vd gia hạn EndDate). Định nghĩa ở BC tiêu thụ
// (Course), implement ở BC sở hữu Attendance (Enrollment) — giữ ranh giới cross-BC qua interface.
public interface IEnrollmentAttendanceSync {
    // Đảm bảo mỗi Student đã enroll có Attendance cho MỌI ClassSession thuộc các WeeklySlot
    // họ đã chọn — chỉ tạo bản còn THIẾU (idempotent). Nếu không back-fill, các buổi mới sinh
    // ra cho slot student đã chọn sẽ không có Attendance → Lecturer mở buổi đó thấy trống.
    Task BackfillAttendanceForCourseAsync(Guid courseId, CancellationToken cancellationToken = default);
}
