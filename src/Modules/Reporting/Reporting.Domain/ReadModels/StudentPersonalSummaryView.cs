namespace Reporting.Domain.ReadModels;

// Projection được cập nhật bởi: StudentEnrolledEvent (thêm row),
// CourseStatusChangedEvent (cập nhật status), GradeAssignedEvent / GradeUpdatedEvent (grade),
// AttendanceMarkedEvent (presentCount / excusedCount / absentCount).
// overallGpa và attendanceRate là Derived Fields — KHÔNG lưu vào bảng này.
// Tính tại Application Layer khi assemble GetMyPersonalSummary response:
//   overallGpa      = SUM(grade) / COUNT(grade IS NOT NULL)
//   attendanceRate  = (presentCount + excusedCount) / totalSessionsEnded
//                     chỉ tính ca có sessionDate <= today.
public class StudentPersonalSummaryView {
    public Guid StudentId { get; private set; }
    public Guid CourseId { get; private set; }
    public string CourseName { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;

    // Null khi Course chưa được chấm điểm.
    public decimal? Grade { get; private set; }

    // totalSessions = số ca Student đã chọn (luôn = 2 theo business rule).
    public int TotalSessions { get; private set; }
    public int PresentCount { get; private set; }
    public int ExcusedCount { get; private set; }
    public int AbsentCount { get; private set; }

    // Required by EF Core.
    private StudentPersonalSummaryView() { }

    public static StudentPersonalSummaryView Create(
        Guid studentId,
        Guid courseId,
        string courseName,
        string status,
        int totalSessions) {
        return new StudentPersonalSummaryView {
            StudentId = studentId,
            CourseId = courseId,
            CourseName = courseName,
            Status = status,
            Grade = null,
            TotalSessions = totalSessions,
            PresentCount = 0,
            ExcusedCount = 0,
            AbsentCount = 0
        };
    }

    // Được gọi ngay sau Create() để đồng bộ với trạng thái Absent mặc định
    // mà EnrollCourse handler đã pre-populate cho tất cả ClassSessions của Course.
    // absentCount = tổng số ClassSession của Course (không phải chỉ 2 ca đã chọn).
    public void InitializeAbsentCount(int absentCount) {
        AbsentCount = absentCount;
    }

    public void UpdateGrade(decimal grade) {
        Grade = grade;
    }

    public void UpdateStatus(string newStatus) {
        Status = newStatus;
    }

    public void UpdateCourseName(string courseName) {
        CourseName = courseName;
    }

    // Được gọi khi AttendanceMarkedEvent xảy ra.
    public void UpdateAttendance(string previousStatus, string newStatus) {
        DecrementCounter(previousStatus);
        IncrementCounter(newStatus);
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