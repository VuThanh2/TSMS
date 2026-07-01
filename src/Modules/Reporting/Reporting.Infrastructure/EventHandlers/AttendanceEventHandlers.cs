using EnrollmentManagement.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Reporting.Domain.Repositories;

namespace Reporting.Infrastructure.EventHandlers;

// Lắng nghe AttendanceMarkedEvent từ EnrollmentManagement BC.
// PreviousStatus đã có sẵn trong event (được capture tại Attendance.Mark())
// nên handler không cần query ngược lại Enrollment BC.
public class AttendanceEventHandler : INotificationHandler<AttendanceMarkedEvent> {
    private readonly IReportingRepository _repository;
    private readonly ILogger<AttendanceEventHandler> _logger;

    public AttendanceEventHandler(
        IReportingRepository repository,
        ILogger<AttendanceEventHandler> logger) {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(
        AttendanceMarkedEvent notification,
        CancellationToken cancellationToken) {
        var previousStatus = notification.PreviousStatus.ToString();
        var newStatus = notification.NewStatus.ToString();

        // 1. CourseAttendanceReportView
        var attendanceReport = await _repository.GetAttendanceReportByStudentAndCourseAsync(
            notification.StudentId, notification.CourseId, cancellationToken);

        if (attendanceReport is not null)
            attendanceReport.UpdateAttendance(previousStatus, newStatus);
        else
            _logger.LogWarning(
                "CourseAttendanceReportView not found. StudentId: {StudentId}, CourseId: {CourseId}",
                notification.StudentId, notification.CourseId);

        // 2. StudentPersonalSummaryView
        var summary = await _repository.GetPersonalSummaryAsync(
            notification.StudentId, notification.CourseId, cancellationToken);

        if (summary is not null)
            summary.UpdateAttendance(previousStatus, newStatus);
        else
            _logger.LogWarning(
                "StudentPersonalSummaryView not found. StudentId: {StudentId}, CourseId: {CourseId}",
                notification.StudentId, notification.CourseId);

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated attendance {Prev} → {New} for StudentId {StudentId}, CourseId {CourseId}.",
            previousStatus, newStatus, notification.StudentId, notification.CourseId);
    }
}