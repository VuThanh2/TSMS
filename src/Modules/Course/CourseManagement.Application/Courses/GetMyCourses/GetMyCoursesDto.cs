namespace CourseManagement.Application.Courses.GetMyCourses;

public sealed record GetMyCoursesOutputDto(
    Guid CourseId,
    string Name,
    string Status,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid LecturerId,
    string? LecturerName,
    decimal? Grade);