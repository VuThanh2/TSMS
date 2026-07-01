using Reporting.Application.Attendance.GetCourseAttendanceReport;
using Reporting.Application.CourseStatistics.GetCourseStatistics;
using Reporting.Application.PersonalSummary.GetMyPersonalSummary;
using Reporting.Application.ScoreDistribution.GetScoreDistribution;
using Reporting.Application.StudentGrades.GetStudentGrades;
using Reporting.Domain.ReadModels;

namespace Reporting.Application.Common.Mappers;

public static class ReportingMapper {
    // ── GetCourseStatistics

    public static CourseStatisticsItemDto ToCourseStatisticsItemDto(CourseStatisticsView view) =>
        new(
            CourseId: view.CourseId,
            CourseName: view.CourseName,
            LecturerName: view.LecturerName,
            StartDate: view.StartDate,
            EndDate: view.EndDate,
            Status: view.Status,
            EnrolledCount: view.EnrolledCount,
            AverageScore: view.AverageScore,
            GradedStudentCount: view.GradedStudentCount,
            UngradedStudentCount: view.UngradedStudentCount);

    // ── GetStudentGrades

    public static StudentGradeItemDto ToStudentGradeItemDto(StudentGradeReportView view) =>
        new(
            EnrollmentId: view.EnrollmentId,
            StudentId: view.StudentId,
            StudentFullName: view.StudentFullName,
            StudentEmail: view.StudentEmail,
            Grade: view.Grade);

    // ── GetScoreDistribution

    public static ScoreDistributionItemDto ToScoreDistributionItemDto(
        CourseScoreDistributionView view) =>
        new(
            ScoreGroup: view.ScoreGroup,
            RangeStart: view.RangeStart,
            RangeEnd: view.RangeEnd,
            StudentCount: view.StudentCount,
            Percentage: view.Percentage);

    // ── GetCourseAttendanceReport

    public static CourseAttendanceItemDto ToCourseAttendanceItemDto(
        CourseAttendanceReportView view,
        int endedSessionCount) =>
        new(
            EnrollmentId: view.EnrollmentId,
            StudentId: view.StudentId,
            StudentFullName: view.StudentFullName,
            StudentEmail: view.StudentEmail,
            TotalSessions: view.TotalSessions,
            PresentCount: view.PresentCount,
            ExcusedCount: view.ExcusedCount,
            AbsentCount: view.AbsentCount,
            AttendanceRate: CalculateAttendanceRate(
                view.PresentCount, view.ExcusedCount, endedSessionCount));

    // ── GetMyPersonalSummary

    public static PersonalSummaryItemDto ToPersonalSummaryItemDto(
        StudentPersonalSummaryView view,
        int endedSessionCount) =>
        new(
            CourseId: view.CourseId,
            CourseName: view.CourseName,
            Status: view.Status,
            Grade: view.Grade,
            TotalSessions: view.TotalSessions,
            PresentCount: view.PresentCount,
            ExcusedCount: view.ExcusedCount,
            AbsentCount: view.AbsentCount,
            AttendanceRate: CalculateAttendanceRate(
                view.PresentCount, view.ExcusedCount, endedSessionCount));

    // ── Private helpers

    // attendanceRate = (presentCount + excusedCount) / totalSessionsEnded.
    // totalSessionsEnded = 0 (chưa có ca nào diễn ra) → trả về 0 để tránh chia cho 0.
    private static decimal CalculateAttendanceRate(
        int presentCount,
        int excusedCount,
        int endedSessionCount) =>
        endedSessionCount > 0
            ? Math.Round((decimal)(presentCount + excusedCount) / endedSessionCount, 4)
            : 0m;
}