using EnrollmentManagement.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Reporting.Domain.ReadModels;
using Reporting.Domain.Repositories;

namespace Reporting.Infrastructure.EventHandlers;

// Lắng nghe tất cả events từ EnrollmentManagement BC liên quan đến Reporting.
// MediatR dispatch đến đây sau khi EnrollmentOutboxProcessor publish.
public class EnrollmentEventHandler :
    INotificationHandler<StudentEnrolledEvent>,
    INotificationHandler<GradeAssignedEvent>,
    INotificationHandler<GradeUpdatedEvent> {

    private readonly IReportingRepository _repository;
    private readonly ILogger<EnrollmentEventHandler> _logger;

    public EnrollmentEventHandler(
        IReportingRepository repository,
        ILogger<EnrollmentEventHandler> logger) {
        _repository = repository;
        _logger = logger;
    }

    // StudentEnrolledEvent → khởi tạo row trên 4 views cùng lúc.
    // Outbox là at-least-once delivery nên event này có thể tới trùng (redelivered) — check
    // StudentGradeReportView theo EnrollmentId TRƯỚC khi ghi bất kỳ side-effect nào (kể cả
    // IncrementEnrolledCount) để đảm bảo idempotent, tránh đếm trùng EnrolledCount.
    public async Task Handle(
        StudentEnrolledEvent notification,
        CancellationToken cancellationToken) {
        var existingGradeReport = await _repository.GetStudentGradeReportAsync(
            notification.EnrollmentId, cancellationToken);

        if (existingGradeReport is not null) {
            _logger.LogWarning(
                "Reporting projections already initialized for EnrollmentId {EnrollmentId}. Skipping duplicate StudentEnrolledEvent.",
                notification.EnrollmentId);
            return;
        }

        // 1. CourseStatisticsView
        var stats = await _repository.GetCourseStatisticsAsync(
            notification.CourseId, cancellationToken);

        if (stats is not null)
            stats.IncrementEnrolledCount();
        else
            _logger.LogWarning(
                "CourseStatisticsView not found for StudentEnrolledEvent. CourseId: {CourseId}",
                notification.CourseId);

        // 2. StudentGradeReportView
        _repository.AddStudentGradeReport(StudentGradeReportView.Create(
            enrollmentId: notification.EnrollmentId,
            courseId: notification.CourseId,
            courseName: notification.CourseName,
            studentId: notification.StudentId,
            studentFullName: notification.StudentFullName,
            studentEmail: notification.StudentEmail));

        // 3. CourseAttendanceReportView
        var attendanceReport = CourseAttendanceReportView.Create(
            enrollmentId: notification.EnrollmentId,
            courseId: notification.CourseId,
            courseName: notification.CourseName,
            studentId: notification.StudentId,
            studentFullName: notification.StudentFullName,
            studentEmail: notification.StudentEmail,
            totalSessions: 2);

        attendanceReport.InitializeAbsentCount(notification.TotalSessionsInCourse);
        _repository.AddAttendanceReport(attendanceReport);

        // 4. StudentPersonalSummaryView
        var existingSummary = await _repository.GetPersonalSummaryAsync(
            notification.StudentId, notification.CourseId, cancellationToken);

        if (existingSummary is null) {
            var summary = StudentPersonalSummaryView.Create(
                studentId: notification.StudentId,
                courseId: notification.CourseId,
                courseName: notification.CourseName,
                status: notification.CourseStatus,
                totalSessions: 2);

            summary.InitializeAbsentCount(notification.TotalSessionsInCourse);
            _repository.AddPersonalSummary(summary);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Initialized Reporting projections for EnrollmentId {EnrollmentId}.",
            notification.EnrollmentId);
    }

    // GradeAssignedEvent → cập nhật grade trên 4 views.
    // previousGrade = null vì đây là lần chấm điểm đầu tiên.
    public async Task Handle(
        GradeAssignedEvent notification,
        CancellationToken cancellationToken) {
        var gradeReport = await _repository.GetStudentGradeReportAsync(
            notification.EnrollmentId, cancellationToken);
        gradeReport?.UpdateGrade(notification.Grade);

        var summary = await _repository.GetPersonalSummaryAsync(
            notification.StudentId, notification.CourseId, cancellationToken);
        summary?.UpdateGrade(notification.Grade);

        var stats = await _repository.GetCourseStatisticsAsync(
            notification.CourseId, cancellationToken);
        stats?.RecalculateGradeStats(previousGrade: null, newGrade: notification.Grade);

        await RebuildScoreDistributionAsync(
            notification.CourseId, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Applied GradeAssigned for EnrollmentId {EnrollmentId}, Grade {Grade}.",
            notification.EnrollmentId, notification.Grade);
    }

    // GradeUpdatedEvent → tương tự GradeAssigned nhưng truyền previousGrade.
    public async Task Handle(
        GradeUpdatedEvent notification,
        CancellationToken cancellationToken) {
        var gradeReport = await _repository.GetStudentGradeReportAsync(
            notification.EnrollmentId, cancellationToken);
        gradeReport?.UpdateGrade(notification.NewGrade);

        var summary = await _repository.GetPersonalSummaryAsync(
            notification.StudentId, notification.CourseId, cancellationToken);
        summary?.UpdateGrade(notification.NewGrade);

        var stats = await _repository.GetCourseStatisticsAsync(
            notification.CourseId, cancellationToken);
        stats?.RecalculateGradeStats(
            previousGrade: notification.PreviousGrade,
            newGrade: notification.NewGrade);

        await RebuildScoreDistributionAsync(
            notification.CourseId, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Applied GradeUpdated for EnrollmentId {EnrollmentId}, {Prev} → {New}.",
            notification.EnrollmentId, notification.PreviousGrade, notification.NewGrade);
    }

    // Rebuild toàn bộ ScoreDistribution của Course sau mỗi lần grade thay đổi.
    // Đọc tất cả grades hiện tại từ StudentGradeReportView (đã được update ở bước trên),
    // đếm lại theo từng ScoreGroup rồi gọi UpdateCount().
    private async Task RebuildScoreDistributionAsync(
        Guid courseId,
        CancellationToken cancellationToken) {
        var distributions = await _repository.GetScoreDistributionByCourseAsync(
            courseId, cancellationToken);

        if (distributions.Count == 0)
            return;

        var allGrades = await _repository.GetStudentGradesByCourseAsync(
            courseId, cancellationToken);

        var gradedValues = allGrades
            .Where(r => r.Grade.HasValue)
            .Select(r => r.Grade!.Value)
            .ToList();

        var totalGraded = gradedValues.Count;

        foreach (var dist in distributions) {
            var count = gradedValues
                .Count(g => CourseScoreDistributionView.ResolveScoreGroup(g) == dist.ScoreGroup);
            dist.UpdateCount(count, totalGraded);
        }
    }
}