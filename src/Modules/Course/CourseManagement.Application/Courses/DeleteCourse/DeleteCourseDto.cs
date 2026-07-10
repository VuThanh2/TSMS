namespace CourseManagement.Application.Courses.DeleteCourse;

// Command style — CourseId đến từ route, không cần InputDto.
public sealed record DeleteCourseOutputDto(bool Success);
