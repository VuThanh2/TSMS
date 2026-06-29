namespace EnrollmentManagement.Application.Grading.GradeStudent;

public sealed record GradeStudentInputDto(decimal Grade);

public sealed record GradeStudentOutputDto(
    Guid EnrollmentId,
    Guid StudentId,
    Guid CourseId,
    decimal Grade);