namespace Reporting.Domain.ReadModels;

// Projection được cập nhật bởi: StudentEnrolledEvent (thêm row, grade = null),
// GradeAssignedEvent / GradeUpdatedEvent (cập nhật grade).
// grade là null nếu Student chưa được nhập điểm.
public class StudentGradeReportView {
    public Guid EnrollmentId { get; private set; }
    public Guid CourseId { get; private set; }
    public string CourseName { get; private set; } = string.Empty;
    public Guid StudentId { get; private set; }
 
    // Denormalized từ Identity BC — cập nhật khi UserUpdatedEvent fire.
    public string StudentFullName { get; private set; } = string.Empty;
    public string StudentEmail { get; private set; } = string.Empty;
 
    // Null khi Student chưa được nhập điểm.
    public decimal? Grade { get; private set; }
 
    // Required by EF Core.
    private StudentGradeReportView() { }
 
    public static StudentGradeReportView Create(
        Guid enrollmentId,
        Guid courseId,
        string courseName,
        Guid studentId,
        string studentFullName,
        string studentEmail) {
        return new StudentGradeReportView {
            EnrollmentId = enrollmentId,
            CourseId = courseId,
            CourseName = courseName,
            StudentId = studentId,
            StudentFullName = studentFullName,
            StudentEmail = studentEmail,
            Grade = null
        };
    }
 
    public void UpdateGrade(decimal grade) {
        Grade = grade;
    }
 
    public void UpdateCourseName(string courseName) {
        CourseName = courseName;
    }
 
    // Được gọi khi UserUpdatedEvent fire (Student đổi tên hoặc email).
    public void UpdateStudentInfo(string fullName, string email) {
        StudentFullName = fullName;
        StudentEmail = email;
    }
}