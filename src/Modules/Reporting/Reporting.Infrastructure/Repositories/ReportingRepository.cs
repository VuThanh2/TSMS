using Microsoft.EntityFrameworkCore;
using Reporting.Domain.ReadModels;
using Reporting.Domain.Repositories;
using Reporting.Infrastructure.Persistence;

namespace Reporting.Infrastructure.Repositories;

public class ReportingRepository : IReportingRepository {
    private readonly ReportingDbContext _context;

    public ReportingRepository(ReportingDbContext context) {
        _context = context;
    }

    // ── CourseStatisticsView

    public async Task<CourseStatisticsView?> GetCourseStatisticsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.CourseStatistics
            .FirstOrDefaultAsync(v => v.CourseId == courseId, cancellationToken);
    }

    public async Task<List<CourseStatisticsView>> GetAllCourseStatisticsAsync(
        CancellationToken cancellationToken = default) {
        return await _context.CourseStatistics
            .OrderBy(v => v.CourseName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CourseStatisticsView>> GetCourseStatisticsByLecturerIdAsync(
        Guid lecturerId,
        CancellationToken cancellationToken = default) {
        return await _context.CourseStatistics
            .Where(v => v.LecturerId == lecturerId)
            .ToListAsync(cancellationToken);
    }

    public void AddCourseStatistics(CourseStatisticsView view) {
        _context.CourseStatistics.Add(view);
    }

    // ── StudentGradeReportView

    public async Task<StudentGradeReportView?> GetStudentGradeReportAsync(
        Guid enrollmentId,
        CancellationToken cancellationToken = default) {
        return await _context.StudentGradeReports
            .FirstOrDefaultAsync(v => v.EnrollmentId == enrollmentId, cancellationToken);
    }

    public async Task<List<StudentGradeReportView>> GetStudentGradesByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.StudentGradeReports
            .Where(v => v.CourseId == courseId)
            .OrderBy(v => v.StudentFullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StudentGradeReportView>> GetStudentGradesByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) {
        return await _context.StudentGradeReports
            .Where(v => v.StudentId == studentId)
            .ToListAsync(cancellationToken);
    }

    public void AddStudentGradeReport(StudentGradeReportView view) {
        _context.StudentGradeReports.Add(view);
    }

    // ── CourseScoreDistributionView

    public async Task<List<CourseScoreDistributionView>> GetScoreDistributionByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.ScoreDistributions
            .Where(v => v.CourseId == courseId)
            .OrderByDescending(v => v.RangeStart)
            .ToListAsync(cancellationToken);
    }

    public void AddScoreDistribution(CourseScoreDistributionView view) {
        _context.ScoreDistributions.Add(view);
    }

    // ── CourseAttendanceReportView

    public async Task<CourseAttendanceReportView?> GetAttendanceReportAsync(
        Guid enrollmentId,
        CancellationToken cancellationToken = default) {
        return await _context.AttendanceReports
            .FirstOrDefaultAsync(v => v.EnrollmentId == enrollmentId, cancellationToken);
    }

    public async Task<CourseAttendanceReportView?> GetAttendanceReportByStudentAndCourseAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.AttendanceReports
            .FirstOrDefaultAsync(
                v => v.StudentId == studentId && v.CourseId == courseId,
                cancellationToken);
    }

    public async Task<List<CourseAttendanceReportView>> GetAttendanceReportByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.AttendanceReports
            .Where(v => v.CourseId == courseId)
            .OrderBy(v => v.StudentFullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CourseAttendanceReportView>> GetAttendanceReportsByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) {
        return await _context.AttendanceReports
            .Where(v => v.StudentId == studentId)
            .ToListAsync(cancellationToken);
    }

    public void AddAttendanceReport(CourseAttendanceReportView view) {
        _context.AttendanceReports.Add(view);
    }

    // ── StudentPersonalSummaryView

    public async Task<StudentPersonalSummaryView?> GetPersonalSummaryAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default) {
        return await _context.PersonalSummaries
            .FirstOrDefaultAsync(
                v => v.StudentId == studentId && v.CourseId == courseId,
                cancellationToken);
    }

    public async Task<List<StudentPersonalSummaryView>> GetPersonalSummariesByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) {
        return await _context.PersonalSummaries
            .Where(v => v.StudentId == studentId)
            .OrderBy(v => v.CourseName)
            .ToListAsync(cancellationToken);
    }

    public void AddPersonalSummary(StudentPersonalSummaryView view) {
        _context.PersonalSummaries.Add(view);
    }

    // ── Persistence

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) {
        await _context.SaveChangesAsync(cancellationToken);
    }
}