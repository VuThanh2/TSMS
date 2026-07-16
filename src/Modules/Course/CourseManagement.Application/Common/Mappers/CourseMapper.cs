using CourseManagement.Application.ClassSessions.GetClassSessions;
using CourseManagement.Application.Courses.CreateCourse;
using CourseManagement.Application.Courses.GetAvailableCourses;
using CourseManagement.Application.Courses.GetCourseById;
using CourseManagement.Application.Courses.GetCourses;
using CourseManagement.Application.Courses.ReplaceLecturer;
using CourseManagement.Application.Courses.UpdateCourse;
using CourseManagement.Domain.Entities;

namespace CourseManagement.Application.Common.Mappers;

public static class CourseMapper {
    public static CreateCourseOutputDto ToCreateCourseOutputDto(Course course, string? lecturerName) =>
        new(
            CourseId: course.Id,
            Name: course.Name,
            Description: course.Description,
            StartDate: course.StartDate,
            EndDate: course.EndDate,
            Status: course.Status.ToString(),
            MaxCapacity: course.MaxCapacity,
            LecturerId: course.LecturerId,
            LecturerName: lecturerName,
            CreatedAt: course.CreatedAt);
 
    public static UpdateCourseOutputDto ToUpdateCourseOutputDto(Course course, string? lecturerName) =>
        new(
            CourseId: course.Id,
            Name: course.Name,
            Description: course.Description,
            StartDate: course.StartDate,
            EndDate: course.EndDate,
            Status: course.Status.ToString(),
            MaxCapacity: course.MaxCapacity,
            LecturerId: course.LecturerId,
            LecturerName: lecturerName);
 
    public static ReplaceLecturerOutputDto ToReplaceLecturerOutputDto(Course course, string? lecturerName) =>
        new(
            CourseId: course.Id,
            Name: course.Name,
            LecturerId: course.LecturerId,
            LecturerName: lecturerName,
            Status: course.Status.ToString());
 
    // ── List projections (no sessions loaded)
    
    public static GetCoursesOutputDto ToGetCoursesOutputDto(
        Course course,
        string? lecturerName,
        int enrolledCount) =>
        new(
            CourseId: course.Id,
            Name: course.Name,
            Description: course.Description,
            StartDate: course.StartDate,
            EndDate: course.EndDate,
            Status: course.Status.ToString(),
            MaxCapacity: course.MaxCapacity,
            EnrolledCount: enrolledCount,
            LecturerId: course.LecturerId,
            LecturerName: lecturerName,
            CreatedAt: course.CreatedAt);

    public static GetAvailableCoursesOutputDto ToGetAvailableCoursesOutputDto(
        Course course,
        string? lecturerName,
        int enrolledCount) =>
        new(
            CourseId: course.Id,
            Name: course.Name,
            Description: course.Description,
            StartDate: course.StartDate,
            EndDate: course.EndDate,
            MaxCapacity: course.MaxCapacity,
            EnrolledCount: enrolledCount,
            LecturerId: course.LecturerId,
            LecturerName: lecturerName);

    // ── Detail projection (sessions must be loaded)

    public static GetCourseByIdOutputDto ToGetCourseByIdOutputDto(
        Course course,
        string? lecturerName,
        int enrolledCount) =>
        new(
            CourseId: course.Id,
            Name: course.Name,
            Description: course.Description,
            StartDate: course.StartDate,
            EndDate: course.EndDate,
            Status: course.Status.ToString(),
            IsOpenForEnrollment: course.IsOpenForEnrollment,
            MaxCapacity: course.MaxCapacity,
            EnrolledCount: enrolledCount,
            LecturerId: course.LecturerId,
            LecturerName: lecturerName,
            CreatedAt: course.CreatedAt,
            ClassSessions: course.ClassSessions
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.SessionType)
                .Select(ClassSessionMapper.ToGetClassSessionsOutputDto)
                .ToList());
}