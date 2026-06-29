namespace EnrollmentManagement.Application.Enrollments.GetCourseEnrollments;

public sealed record GetCourseEnrollmentsOutputDto(
    Guid EnrollmentId,
    Guid StudentId,
    string? StudentFullName,
    string? StudentEmail,
    decimal? Grade);