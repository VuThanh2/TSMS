using CourseManagement.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Reporting.Domain.ReadModels;
using Reporting.Domain.Repositories;

namespace Reporting.Infrastructure.EventHandlers;

// Lắng nghe tất cả events từ CourseManagement BC liên quan đến Reporting.
// MediatR dispatch đến đây sau khi CourseOutboxProcessor publish.
public class CourseEventHandler :
    INotificationHandler<CourseCreatedEvent>,
    INotificationHandler<CourseUpdatedEvent>,
    INotificationHandler<CourseStatusChangedEvent>,
    INotificationHandler<LecturerReplacedEvent>,
    INotificationHandler<CourseDeletedEvent> {
 
    private readonly IReportingRepository _repository;
    private readonly ILogger<CourseEventHandler> _logger;
 
    public CourseEventHandler(
        IReportingRepository repository,
        ILogger<CourseEventHandler> logger) {
        _repository = repository;
        _logger = logger;
    }
 
    // CourseCreatedEvent → khởi tạo CourseStatisticsView + 4 ScoreDistribution rows.
    public async Task Handle(
        CourseCreatedEvent notification,
        CancellationToken cancellationToken) {
        var existing = await _repository.GetCourseStatisticsAsync(
            notification.CourseId, cancellationToken);
 
        if (existing is not null) {
            _logger.LogWarning(
                "CourseStatisticsView already exists for CourseId {CourseId}. Skipping.",
                notification.CourseId);
            return;
        }
 
        var view = CourseStatisticsView.Create(
            courseId: notification.CourseId,
            lecturerId: notification.LecturerId,
            courseName: notification.CourseName,
            lecturerName: notification.LecturerName,
            startDate: notification.StartDate,
            endDate: notification.EndDate,
            status: "Upcoming");
 
        _repository.AddCourseStatistics(view);
 
        foreach (var (group, start, end) in ScoreGroups.All) {
            _repository.AddScoreDistribution(CourseScoreDistributionView.Create(
                courseId: notification.CourseId,
                courseName: notification.CourseName,
                scoreGroup: group,
                rangeStart: start,
                rangeEnd: end));
        }
 
        await _repository.SaveChangesAsync(cancellationToken);
 
        _logger.LogInformation(
            "Initialized Reporting projections for CourseId {CourseId}.",
            notification.CourseId);
    }
 
    // CourseUpdatedEvent → cập nhật CourseName và EndDate trên tất cả views liên quan.
    public async Task Handle(
        CourseUpdatedEvent notification,
        CancellationToken cancellationToken) {
        var stats = await _repository.GetCourseStatisticsAsync(
            notification.CourseId, cancellationToken);
 
        if (stats is null) {
            _logger.LogWarning(
                "CourseStatisticsView not found for CourseUpdatedEvent. CourseId: {CourseId}",
                notification.CourseId);
            return;
        }
 
        stats.UpdateCourseInfo(notification.CourseName, notification.EndDate);
 
        var gradeReports = await _repository.GetStudentGradesByCourseAsync(
            notification.CourseId, cancellationToken);
        foreach (var r in gradeReports)
            r.UpdateCourseName(notification.CourseName);
 
        var distributions = await _repository.GetScoreDistributionByCourseAsync(
            notification.CourseId, cancellationToken);
        foreach (var d in distributions)
            d.UpdateCourseName(notification.CourseName);
 
        var attendanceReports = await _repository.GetAttendanceReportByCourseAsync(
            notification.CourseId, cancellationToken);
        foreach (var r in attendanceReports)
            r.UpdateCourseName(notification.CourseName);
 
        // PersonalSummaryView cũng lưu CourseName — cần cập nhật đồng bộ.
        foreach (var r in attendanceReports) {
            var summary = await _repository.GetPersonalSummaryAsync(
                r.StudentId, notification.CourseId, cancellationToken);
            summary?.UpdateCourseName(notification.CourseName);
        }
 
        await _repository.SaveChangesAsync(cancellationToken);
 
        _logger.LogInformation(
            "Updated CourseName/EndDate projections for CourseId {CourseId}.",
            notification.CourseId);
    }
 
    // CourseStatusChangedEvent → cập nhật Status trên CourseStatisticsView
    // và StudentPersonalSummaryView của tất cả Student đã enroll.
    public async Task Handle(
        CourseStatusChangedEvent notification,
        CancellationToken cancellationToken) {
        var stats = await _repository.GetCourseStatisticsAsync(
            notification.CourseId, cancellationToken);
 
        if (stats is null) {
            _logger.LogWarning(
                "CourseStatisticsView not found for CourseStatusChangedEvent. CourseId: {CourseId}",
                notification.CourseId);
            return;
        }
 
        var newStatus = notification.NewStatus.ToString();
        stats.UpdateStatus(newStatus);
 
        var attendanceReports = await _repository.GetAttendanceReportByCourseAsync(
            notification.CourseId, cancellationToken);
 
        foreach (var report in attendanceReports) {
            var summary = await _repository.GetPersonalSummaryAsync(
                report.StudentId, notification.CourseId, cancellationToken);
            summary?.UpdateStatus(newStatus);
        }
 
        await _repository.SaveChangesAsync(cancellationToken);
 
        _logger.LogInformation(
            "Updated Status to {Status} for CourseId {CourseId}.",
            newStatus, notification.CourseId);
    }
 
    // CourseDeletedEvent → dọn projection của course bị xóa. Course chỉ xóa được khi Upcoming
    // và chưa có Student enroll, nên chỉ tồn tại CourseStatisticsView + 4 ScoreDistribution rows
    // (chưa có grade/attendance/summary). Xóa 2 loại này là đủ, không để lại phantom trong thống kê.
    public async Task Handle(
        CourseDeletedEvent notification,
        CancellationToken cancellationToken) {
        var stats = await _repository.GetCourseStatisticsAsync(
            notification.CourseId, cancellationToken);

        if (stats is null) {
            _logger.LogWarning(
                "CourseStatisticsView not found for CourseDeletedEvent. CourseId: {CourseId}",
                notification.CourseId);
            return;
        }

        _repository.RemoveCourseStatistics(stats);

        var distributions = await _repository.GetScoreDistributionByCourseAsync(
            notification.CourseId, cancellationToken);
        if (distributions.Count > 0)
            _repository.RemoveScoreDistributions(distributions);

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Removed Reporting projections for deleted CourseId {CourseId}.",
            notification.CourseId);
    }

    // LecturerReplacedEvent → cập nhật LecturerId và LecturerName trên CourseStatisticsView.
    // NewLecturerName đã được enrich vào event tại ReplaceLecturerCommandHandler.
    public async Task Handle(
        LecturerReplacedEvent notification,
        CancellationToken cancellationToken) {
        var stats = await _repository.GetCourseStatisticsAsync(
            notification.CourseId, cancellationToken);
 
        if (stats is null) {
            _logger.LogWarning(
                "CourseStatisticsView not found for LecturerReplacedEvent. CourseId: {CourseId}",
                notification.CourseId);
            return;
        }
 
        // UpdateLecturer() sync cả LecturerId để UserUpdatedEvent handler
        // có thể query đúng Course khi Lecturer đổi tên sau này.
        stats.UpdateLecturer(notification.NewLecturerId, notification.NewLecturerName);
        await _repository.SaveChangesAsync(cancellationToken);
 
        _logger.LogInformation(
            "Updated Lecturer to '{LecturerName}' for CourseId {CourseId}.",
            notification.NewLecturerName, notification.CourseId);
    }
}