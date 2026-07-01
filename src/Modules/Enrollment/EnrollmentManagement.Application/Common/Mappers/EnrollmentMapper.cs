using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Enrollments.GetCourseEnrollments;
using EnrollmentManagement.Application.Enrollments.GetMyEnrollments;
using EnrollmentManagement.Application.Grading.GradeStudent;
using EnrollmentManagement.Domain.Entities;
using AdjustSessionDtos = EnrollmentManagement.Application.Enrollments.AdjustSession;
using EnrollCourseDtos = EnrollmentManagement.Application.Enrollments.EnrollCourse;

namespace EnrollmentManagement.Application.Common.Mappers;

public static class EnrollmentMapper {
    // ── EnrollCourse

    public static EnrollCourseDtos.EnrollCourseOutputDto ToEnrollCourseOutputDto(
        Enrollment enrollment,
        IReadOnlyList<ClassSessionLookup> sessionLookups) =>
        new(
            EnrollmentId: enrollment.Id,
            CourseId: enrollment.CourseId,
            StudentId: enrollment.StudentId,
            EnrolledAt: enrollment.EnrolledAt,
            EnrolledSessions: enrollment.EnrolledSessions
                .Select(s => ToEnrollCourseSessionOutputDto(s, sessionLookups))
                .ToList());

    // ── AdjustSession

    public static AdjustSessionDtos.AdjustSessionOutputDto ToAdjustSessionOutputDto(
        Enrollment enrollment,
        IReadOnlyList<ClassSessionLookup> sessionLookups) =>
        new(
            EnrollmentId: enrollment.Id,
            EnrolledSessions: enrollment.EnrolledSessions
                .Select(s => ToAdjustSessionEnrolledSessionOutputDto(s, sessionLookups))
                .ToList());

    // ── GradeStudent

    public static GradeStudentOutputDto ToGradeStudentOutputDto(Enrollment enrollment) =>
        new(
            EnrollmentId: enrollment.Id,
            StudentId: enrollment.StudentId,
            CourseId: enrollment.CourseId,
            Grade: enrollment.Grade!.Value);

    // ── GetMyEnrollments

    public static GetMyEnrollmentsOutputDto ToGetMyEnrollmentsOutputDto(
        Enrollment enrollment,
        string courseName) =>
        new(
            EnrollmentId: enrollment.Id,
            CourseId: enrollment.CourseId,
            CourseName: courseName,
            Status: enrollment.Status.ToString(),
            Grade: enrollment.Grade?.Value);

    // ── GetCourseEnrollments

    public static GetCourseEnrollmentsOutputDto ToGetCourseEnrollmentsOutputDto(
        Enrollment enrollment,
        string? studentFullName,
        string? studentEmail) =>
        new(
            EnrollmentId: enrollment.Id,
            StudentId: enrollment.StudentId,
            StudentFullName: studentFullName,
            StudentEmail: studentEmail,
            Grade: enrollment.Grade?.Value);

    // ── Private helpers — tách riêng cho từng use case để tránh ambiguous type

    private static EnrollCourseDtos.EnrolledSessionOutputDto ToEnrollCourseSessionOutputDto(
        EnrolledSession session,
        IReadOnlyList<ClassSessionLookup> sessionLookups) {
        var lookup = sessionLookups.FirstOrDefault(l => l.ClassSessionId == session.ClassSessionId);
        return new EnrollCourseDtos.EnrolledSessionOutputDto(
            EnrolledSessionId: session.Id,
            ClassSessionId: session.ClassSessionId,
            DayOfWeek: lookup is not null ? lookup.SessionDate.DayOfWeek.ToString() : string.Empty,
            SessionType: session.SessionType.ToString());
    }

    private static AdjustSessionDtos.EnrolledSessionOutputDto ToAdjustSessionEnrolledSessionOutputDto(
        EnrolledSession session,
        IReadOnlyList<ClassSessionLookup> sessionLookups) {
        var lookup = sessionLookups.FirstOrDefault(l => l.ClassSessionId == session.ClassSessionId);
        return new AdjustSessionDtos.EnrolledSessionOutputDto(
            EnrolledSessionId: session.Id,
            ClassSessionId: session.ClassSessionId,
            DayOfWeek: lookup is not null ? lookup.SessionDate.DayOfWeek.ToString() : string.Empty,
            SessionType: session.SessionType.ToString());
    }
}