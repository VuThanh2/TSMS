namespace Enrollment.Application.Common.Interfaces;

// Contract cho real-time notification qua SignalR.
// Implement ở Enrollment.Infrastructure (SignalRNotificationService).
// Gửi thông báo đến Student khi Lecturer chấm hoặc cập nhật điểm.
public interface INotificationService {
    // Thông báo đến Student khi điểm được assign hoặc update.
    Task NotifyGradeUpdatedAsync(
        Guid studentId,
        Guid courseId,
        string courseName,
        decimal grade,
        CancellationToken cancellationToken = default);
}