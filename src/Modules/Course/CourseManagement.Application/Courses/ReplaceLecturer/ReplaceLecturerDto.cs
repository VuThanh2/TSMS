namespace CourseManagement.Application.Courses.ReplaceLecturer;

public sealed record ReplaceLecturerInputDto(Guid NewLecturerId);

public sealed record ReplaceLecturerOutputDto(
    Guid CourseId,
    string Name,
    Guid LecturerId,
    string? LecturerName,
    string Status);