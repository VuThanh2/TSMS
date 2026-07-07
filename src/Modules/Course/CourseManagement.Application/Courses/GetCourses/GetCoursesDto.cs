namespace CourseManagement.Application.Courses.GetCourses;

public sealed record GetCoursesOutputDto(
    Guid CourseId,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int MaxCapacity,
    int EnrolledCount,
    Guid LecturerId,
    string? LecturerName,
    DateTime CreatedAt);