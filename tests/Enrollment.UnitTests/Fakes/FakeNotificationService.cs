using EnrollmentManagement.Application.Common.Interfaces;

namespace Enrollment.UnitTests.Fakes;

// Fake INotificationService — ghi lại mỗi lần notify điểm để test GradeStudent assert đã bắn
// SignalR (fire-and-forget) với đúng payload.
public sealed class FakeNotificationService : INotificationService {
    public sealed record GradeNotification(
        Guid StudentId, Guid CourseId, string CourseName, decimal Grade);

    public List<GradeNotification> Notifications { get; } = new();

    public Task NotifyGradeUpdatedAsync(
        Guid studentId, Guid courseId, string courseName, decimal grade,
        CancellationToken cancellationToken = default) {
        Notifications.Add(new GradeNotification(studentId, courseId, courseName, grade));
        return Task.CompletedTask;
    }
}
