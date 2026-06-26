namespace CourseManagement.Application.Courses.GetAvailableCourses;

public sealed record GetAvailableCoursesOutputDto(
    Guid CourseId,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    int MaxCapacity,
    int EnrolledCount,
    Guid LecturerId,
    string? LecturerName);