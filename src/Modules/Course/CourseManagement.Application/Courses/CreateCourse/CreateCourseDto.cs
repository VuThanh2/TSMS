namespace CourseManagement.Application.Courses.CreateCourse;

public sealed record CreateCourseInputDto(
    Guid LecturerId,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    int MaxCapacity);

public sealed record CreateCourseOutputDto(
    Guid CourseId,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int MaxCapacity,
    Guid LecturerId,
    string? LecturerName,
    DateTime CreatedAt);