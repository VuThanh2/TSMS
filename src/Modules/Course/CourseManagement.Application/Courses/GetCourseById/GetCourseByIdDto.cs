using CourseManagement.Application.ClassSessions.GetClassSessions;

namespace CourseManagement.Application.Courses.GetCourseById;

public sealed record GetCourseByIdOutputDto(
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
    DateTime CreatedAt,
    IReadOnlyList<GetClassSessionsOutputDto> ClassSessions);