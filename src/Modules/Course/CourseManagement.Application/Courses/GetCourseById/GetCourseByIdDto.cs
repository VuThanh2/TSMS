using CourseManagement.Application.ClassSessions.GetClassSessions;

namespace CourseManagement.Application.Courses.GetCourseById;

public sealed record GetCourseByIdOutputDto(
    Guid CourseId,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    // Cổng đăng ký — độc lập với Status. false = Admin đang dựng, Student chưa thấy course này.
    bool IsOpenForEnrollment,
    int MaxCapacity,
    int EnrolledCount,
    Guid LecturerId,
    string? LecturerName,
    DateTime CreatedAt,
    IReadOnlyList<GetClassSessionsOutputDto> ClassSessions);