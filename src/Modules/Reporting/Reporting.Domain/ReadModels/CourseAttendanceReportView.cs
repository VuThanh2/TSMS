namespace Reporting.Domain.ReadModels;

// Projection được cập nhật bởi: StudentEnrolledEvent (thêm row),
// AttendanceMarkedEvent (cập nhật presentCount / excusedCount / absentCount).
public class CourseAttendanceReportView {
    public Guid EnrollmentId { get; private set; }
    public Guid CourseId { get; private set; }
    public string CourseName { get; private set; } = string.Empty;
    public Guid StudentId { get; private set; }
 
    // Denormalized từ Identity BC — cập nhật khi UserUpdatedEvent fire.
    public string StudentFullName { get; private set; } = string.Empty;
    public string StudentEmail { get; private set; } = string.Empty;
 
    // totalSessions = số ca Student đã chọn trong Course (luôn = 2 theo business rule).
    public int TotalSessions { get; private set; }
    public int PresentCount { get; private set; }
    public int ExcusedCount { get; private set; }
    public int AbsentCount { get; private set; }
 
    // Required by EF Core.
    private CourseAttendanceReportView() { }
 
    public static CourseAttendanceReportView Create(
        Guid enrollmentId,
        Guid courseId,
        string courseName,
        Guid studentId,
        string studentFullName,
        string studentEmail,
        int totalSessions) {
        return new CourseAttendanceReportView {
            EnrollmentId = enrollmentId,
            CourseId = courseId,
            CourseName = courseName,
            StudentId = studentId,
            StudentFullName = studentFullName,
            StudentEmail = studentEmail,
            TotalSessions = totalSessions,
            PresentCount = 0,
            ExcusedCount = 0,
            AbsentCount = 0
        };
    }
 
    // Được gọi ngay sau Create() để đồng bộ với trạng thái Absent mặc định
    // mà EnrollCourse handler đã pre-populate cho tất cả ClassSessions của Course.
    public void InitializeAbsentCount(int absentCount) {
        AbsentCount = absentCount;
    }
 
    // Được gọi khi AttendanceMarkedEvent xảy ra.
    // previousStatus: trạng thái cũ trước khi mark (capture tại Attendance.Mark()).
    public void UpdateAttendance(string previousStatus, string newStatus) {
        DecrementCounter(previousStatus);
        IncrementCounter(newStatus);
    }
 
    public void UpdateCourseName(string courseName) {
        CourseName = courseName;
    }
 
    // Được gọi khi UserUpdatedEvent fire (Student đổi tên hoặc email).
    public void UpdateStudentInfo(string fullName, string email) {
        StudentFullName = fullName;
        StudentEmail = email;
    }
 
    private void IncrementCounter(string status) {
        switch (status) {
            case "Present": PresentCount++; break;
            case "Excused": ExcusedCount++; break;
            case "Absent":  AbsentCount++;  break;
        }
    }
 
    private void DecrementCounter(string status) {
        switch (status) {
            case "Present": PresentCount = Math.Max(0, PresentCount - 1); break;
            case "Excused": ExcusedCount = Math.Max(0, ExcusedCount - 1); break;
            case "Absent":  AbsentCount  = Math.Max(0, AbsentCount - 1);  break;
        }
    }
}