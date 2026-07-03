namespace EnrollmentManagement.Application.Enrollments.EnrollCourse;

public sealed record EnrollCourseInputDto(
    Guid CourseId,
    IReadOnlyList<Guid> WeeklySlotIds);

public sealed record EnrollCourseOutputDto(
    Guid EnrollmentId,
    Guid CourseId,
    Guid StudentId,
    DateTime EnrolledAt,
    IReadOnlyList<EnrolledSessionOutputDto> EnrolledSessions);

public sealed record EnrolledSessionOutputDto(
    Guid EnrolledSessionId,
    Guid WeeklySlotId,
    string DayOfWeek,
    string SessionType);