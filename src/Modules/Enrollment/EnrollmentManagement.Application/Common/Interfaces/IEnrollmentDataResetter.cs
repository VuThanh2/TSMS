namespace EnrollmentManagement.Application.Common.Interfaces;

/// CHỈ dùng cho Demo Data Reset. Xóa toàn bộ Enrollment/EnrolledSession/Attendance bằng bulk
/// delete trực tiếp — cùng lý do với ICourseDataResetter (CourseManagement BC).
public interface IEnrollmentDataResetter {
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}