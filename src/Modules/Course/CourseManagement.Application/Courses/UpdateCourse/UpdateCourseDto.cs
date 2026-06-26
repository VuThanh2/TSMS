namespace CourseManagement.Application.Courses.UpdateCourse;

public sealed record UpdateCourseInputDto(
    string Name,
    string? Description,
    DateOnly EndDate,
    int MaxCapacity);

public sealed record UpdateCourseOutputDto(
    Guid CourseId,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int MaxCapacity,
    Guid LecturerId,
    string? LecturerName);