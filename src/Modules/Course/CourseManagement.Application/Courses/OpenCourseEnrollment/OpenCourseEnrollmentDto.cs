namespace CourseManagement.Application.Courses.OpenCourseEnrollment;

// Command style — CourseId đến từ route, không cần InputDto.
public sealed record OpenCourseEnrollmentOutputDto(bool IsOpenForEnrollment);
