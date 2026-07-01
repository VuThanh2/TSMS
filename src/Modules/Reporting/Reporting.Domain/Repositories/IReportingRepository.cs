using Reporting.Domain.ReadModels;

namespace Reporting.Domain.Repositories;

public interface IReportingRepository {

    // ── CourseStatisticsView

    Task<CourseStatisticsView?> GetCourseStatisticsAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);

    Task<List<CourseStatisticsView>> GetAllCourseStatisticsAsync(
        CancellationToken cancellationToken = default);
    
    Task<List<CourseStatisticsView>> GetCourseStatisticsByLecturerIdAsync(
        Guid lecturerId,
        CancellationToken cancellationToken = default);

    void AddCourseStatistics(CourseStatisticsView view);

    // ── StudentGradeReportView

    Task<StudentGradeReportView?> GetStudentGradeReportAsync(
        Guid enrollmentId,
        CancellationToken cancellationToken = default);

    Task<List<StudentGradeReportView>> GetStudentGradesByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);
    
    Task<List<StudentGradeReportView>> GetStudentGradesByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    void AddStudentGradeReport(StudentGradeReportView view);

    // ── CourseScoreDistributionView

    Task<List<CourseScoreDistributionView>> GetScoreDistributionByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);

    void AddScoreDistribution(CourseScoreDistributionView view);

    // ── CourseAttendanceReportView

    Task<CourseAttendanceReportView?> GetAttendanceReportAsync(
        Guid enrollmentId,
        CancellationToken cancellationToken = default);

    Task<CourseAttendanceReportView?> GetAttendanceReportByStudentAndCourseAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default);

    Task<List<CourseAttendanceReportView>> GetAttendanceReportByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);
    
    Task<List<CourseAttendanceReportView>> GetAttendanceReportsByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    void AddAttendanceReport(CourseAttendanceReportView view);

    // ── StudentPersonalSummaryView

    Task<StudentPersonalSummaryView?> GetPersonalSummaryAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default);

    Task<List<StudentPersonalSummaryView>> GetPersonalSummariesByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    void AddPersonalSummary(StudentPersonalSummaryView view);

    // ── Persistence

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}