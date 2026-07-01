namespace EnrollmentManagement.Application.Enrollments.GetMyEnrollments;

public sealed record GetMyEnrollmentsOutputDto(
    Guid EnrollmentId,
    Guid CourseId,
    string CourseName,
    string Status,
    decimal? Grade);