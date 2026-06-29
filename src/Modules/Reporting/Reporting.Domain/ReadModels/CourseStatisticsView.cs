namespace Reporting.Domain.ReadModels;

// Projection được cập nhật bởi: CourseCreatedEvent, CourseUpdatedEvent,
// CourseStatusChangedEvent, LecturerReplacedEvent, StudentEnrolledEvent,
// GradeAssignedEvent, GradeUpdatedEvent.
// averageScore chỉ tính trên Student đã được nhập điểm; null nếu chưa có điểm nào.
public class CourseStatisticsView {
    public Guid CourseId { get; private set; }
    public string CourseName { get; private set; } = string.Empty;
 
    // LecturerId lưu để query GetCourseStatisticsByLecturerIdAsync
    // khi UserUpdatedEvent fire — cần biết tất cả Course của Lecturer đó.
    public Guid LecturerId { get; private set; }
 
    // Denormalized từ Identity BC — cập nhật khi LecturerReplacedEvent / UserUpdatedEvent fire.
    public string LecturerName { get; private set; } = string.Empty;
 
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public int EnrolledCount { get; private set; }
    public decimal? AverageScore { get; private set; }
    public int GradedStudentCount { get; private set; }
    public int UngradedStudentCount { get; private set; }
 
    // Required by EF Core.
    private CourseStatisticsView() { }
 
    public static CourseStatisticsView Create(
        Guid courseId,
        Guid lecturerId,
        string courseName,
        string lecturerName,
        DateOnly startDate,
        DateOnly endDate,
        string status) {
        return new CourseStatisticsView {
            CourseId = courseId,
            LecturerId = lecturerId,
            CourseName = courseName,
            LecturerName = lecturerName,
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
            EnrolledCount = 0,
            AverageScore = null,
            GradedStudentCount = 0,
            UngradedStudentCount = 0
        };
    }
 
    public void UpdateCourseInfo(string courseName, DateOnly endDate) {
        CourseName = courseName;
        EndDate = endDate;
    }
 
    public void UpdateStatus(string newStatus) {
        Status = newStatus;
    }
 
    public void UpdateLecturerName(string lecturerName) {
        LecturerName = lecturerName;
    }
 
    public void UpdateLecturer(Guid newLecturerId, string newLecturerName) {
        LecturerId = newLecturerId;
        LecturerName = newLecturerName;
    }
 
    // Được gọi khi StudentEnrolledEvent xảy ra.
    public void IncrementEnrolledCount() {
        EnrolledCount++;
        UngradedStudentCount++;
    }
 
    // Được gọi khi GradeAssignedEvent hoặc GradeUpdatedEvent xảy ra.
    // previousGrade: null khi là lần chấm điểm đầu tiên (GradeAssigned).
    public void RecalculateGradeStats(decimal? previousGrade, decimal newGrade) {
        if (previousGrade is null) {
            GradedStudentCount++;
            UngradedStudentCount = Math.Max(0, UngradedStudentCount - 1);
 
            var previousTotal = AverageScore.HasValue
                ? AverageScore.Value * (GradedStudentCount - 1)
                : 0m;
 
            AverageScore = Math.Round((previousTotal + newGrade) / GradedStudentCount, 2);
        } else {
            var previousTotal = AverageScore.HasValue
                ? AverageScore.Value * GradedStudentCount
                : 0m;
 
            var newTotal = previousTotal - previousGrade.Value + newGrade;
 
            AverageScore = GradedStudentCount > 0
                ? Math.Round(newTotal / GradedStudentCount, 2)
                : null;
        }
    }
}