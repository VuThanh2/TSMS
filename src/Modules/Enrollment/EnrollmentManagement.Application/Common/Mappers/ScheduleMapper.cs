using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Schedules.GetLecturerSchedule;
using EnrollmentManagement.Application.Schedules.GetStudentSchedule;

namespace EnrollmentManagement.Application.Common.Mappers;

public static class ScheduleMapper {
    // ── GetLecturerSchedule

    public static GetLecturerScheduleOutputDto ToGetLecturerScheduleOutputDto(
        ClassSessionLookup session,
        string courseName) =>
        new(
            CourseId: session.CourseId,
            CourseName: courseName,
            ClassSessionId: session.ClassSessionId,
            SessionDate: session.SessionDate,
            DayOfWeek: session.SessionDate.DayOfWeek.ToString(),
            SessionType: session.SessionType);

    // ── GetStudentSchedule

    public static GetStudentScheduleOutputDto ToGetStudentScheduleOutputDto(
        Guid courseId,
        string courseName,
        Guid enrollmentId,
        ClassSessionLookup session,
        string sessionType,
        string attendanceStatus) =>
        new(
            CourseId: courseId,
            CourseName: courseName,
            EnrollmentId: enrollmentId,
            ClassSessionId: session.ClassSessionId,
            SessionDate: session.SessionDate,
            DayOfWeek: session.SessionDate.DayOfWeek.ToString(),
            SessionType: sessionType,
            AttendanceStatus: attendanceStatus);
}