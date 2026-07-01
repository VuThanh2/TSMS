namespace Reporting.Application.StudentGrades.GetStudentGrades;

public sealed record GetStudentGradesOutputDto(
    Guid CourseId,
    string CourseName,
    IReadOnlyList<StudentGradeItemDto> Items);

// Grade là null nếu Student chưa được Lecturer nhập điểm —
// Student chưa có điểm vẫn xuất hiện trong danh sách.
public sealed record StudentGradeItemDto(
    Guid EnrollmentId,
    Guid StudentId,
    string StudentFullName,
    string StudentEmail,
    decimal? Grade);