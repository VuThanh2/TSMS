using Identity.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Reporting.Domain.Repositories;

namespace Reporting.Infrastructure.EventHandlers;

// Lắng nghe UserUpdatedEvent từ Identity BC.
// Identity publish trực tiếp qua IPublisher (không qua Outbox) — handler nhận ngay.
// Chỉ UserUpdatedEvent cần handle vì đây là event duy nhất ảnh hưởng
// đến denormalized fields trong Projection (StudentFullName, StudentEmail, LecturerName).
public class IdentityEventHandler : INotificationHandler<UserUpdatedEvent> {
    private readonly IReportingRepository _repository;
    private readonly ILogger<IdentityEventHandler> _logger;

    public IdentityEventHandler(
        IReportingRepository repository,
        ILogger<IdentityEventHandler> logger) {
        _repository = repository;
        _logger = logger;
    }

    // UserUpdatedEvent → đồng bộ denormalized fields theo Role:
    //   Student  → StudentFullName, StudentEmail trong StudentGradeReportView và CourseAttendanceReportView
    //   Lecturer → LecturerName trong CourseStatisticsView
    //   Admin    → không có Projection nào lưu Admin info → skip
    public async Task Handle(
        UserUpdatedEvent notification,
        CancellationToken cancellationToken) {
        switch (notification.Role) {
            case "Student":
                await SyncStudentInfoAsync(notification, cancellationToken);
                break;
            case "Lecturer":
                await SyncLecturerNameAsync(notification, cancellationToken);
                break;
            default:
                // Admin — không có Projection nào cần sync.
                return;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Synced UserUpdated info for UserId {UserId}, Role {Role}.",
            notification.UserId, notification.Role);
    }

    // ── Private helpers

    private async Task SyncStudentInfoAsync(
        UserUpdatedEvent notification,
        CancellationToken cancellationToken) {
        // 1. StudentGradeReportView — update tất cả rows của Student này.
        var gradeReports = await _repository.GetStudentGradesByStudentIdAsync(
            notification.UserId, cancellationToken);

        foreach (var report in gradeReports)
            report.UpdateStudentInfo(notification.FullName, notification.Email);

        // 2. CourseAttendanceReportView — update tất cả rows của Student này.
        var attendanceReports = await _repository.GetAttendanceReportsByStudentIdAsync(
            notification.UserId, cancellationToken);

        foreach (var report in attendanceReports)
            report.UpdateStudentInfo(notification.FullName, notification.Email);

        _logger.LogInformation(
            "Synced Student info: {Count} GradeReports, {AttCount} AttendanceReports.",
            gradeReports.Count, attendanceReports.Count);
    }

    private async Task SyncLecturerNameAsync(
        UserUpdatedEvent notification,
        CancellationToken cancellationToken) {
        // CourseStatisticsView — update LecturerName trên tất cả Course của Lecturer này.
        var courseStats = await _repository.GetCourseStatisticsByLecturerIdAsync(
            notification.UserId, cancellationToken);

        foreach (var stats in courseStats)
            stats.UpdateLecturerName(notification.FullName);

        _logger.LogInformation(
            "Synced LecturerName for {Count} courses.", courseStats.Count);
    }
}