namespace Reporting.Domain.ReadModels;

// Projection được cập nhật bởi: GradeAssignedEvent / GradeUpdatedEvent.
// Mỗi row = một Score Group của một Course.
// Chỉ tính Student đã được nhập điểm; Student chưa có điểm bị loại trừ hoàn toàn.
// Score Group Boundaries:
//   Xuất sắc  : 9  ≤ grade ≤ 10
//   Giỏi      : 7  ≤ grade < 9
//   Trung bình: 5  ≤ grade < 7
//   Yếu       :      grade < 5
public class CourseScoreDistributionView {
    public Guid Id { get; private set; }
    public Guid CourseId { get; private set; }
    public string CourseName { get; private set; } = string.Empty;
    public string ScoreGroup { get; private set; } = string.Empty;
    public decimal RangeStart { get; private set; }
    public decimal RangeEnd { get; private set; }
    public int StudentCount { get; private set; }

    // percentage = studentCount / gradedStudentCount của Course.
    // Được tính lại mỗi khi GradeAssigned / GradeUpdated xảy ra.
    public decimal Percentage { get; private set; }

    // Required by EF Core.
    private CourseScoreDistributionView() { }

    public static CourseScoreDistributionView Create(
        Guid courseId,
        string courseName,
        string scoreGroup,
        decimal rangeStart,
        decimal rangeEnd) {
        return new CourseScoreDistributionView {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            CourseName = courseName,
            ScoreGroup = scoreGroup,
            RangeStart = rangeStart,
            RangeEnd = rangeEnd,
            StudentCount = 0,
            Percentage = 0m
        };
    }

    // Được gọi sau khi Projection handler đã đếm lại toàn bộ StudentCount
    // cho từng ScoreGroup trong Course, để tính lại Percentage đồng thời.
    public void UpdateCount(int studentCount, int totalGradedStudents) {
        StudentCount = studentCount;
        Percentage = totalGradedStudents > 0
            ? Math.Round((decimal)studentCount / totalGradedStudents, 4)
            : 0m;
    }

    public void UpdateCourseName(string courseName) {
        CourseName = courseName;
    }

    // Helper: xác định Score Group dựa trên điểm số.
    public static string ResolveScoreGroup(decimal grade) {
        return grade switch {
            >= 9m => ScoreGroups.Excellent,
            >= 7m => ScoreGroups.Good,
            >= 5m => ScoreGroups.Average,
            _     => ScoreGroups.Weak
        };
    }
}

// Score Group constants — tránh magic string rải rác.
// All dùng để khởi tạo 4 rows khi CourseCreatedEvent xảy ra.
public static class ScoreGroups {
    public const string Excellent = "Xuất sắc";
    public const string Good      = "Giỏi";
    public const string Average   = "Trung bình";
    public const string Weak      = "Yếu";

    public static readonly IReadOnlyList<(string Group, decimal Start, decimal End)> All = [
        (Excellent, 9m, 10m),
        (Good,      7m,  9m),
        (Average,   5m,  7m),
        (Weak,      0m,  5m)
    ];
}