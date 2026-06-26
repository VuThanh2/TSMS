using Enrollment.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Enrollment.Infrastructure.Services;

// Gửi real-time notification đến Student qua SignalR khi Lecturer chấm / cập nhật điểm.
public class SignalRNotificationService : INotificationService {
    private readonly IHubContext<GradeHub> _hubContext;

    public SignalRNotificationService(IHubContext<GradeHub> hubContext) {
        _hubContext = hubContext;
    }

    // Gửi event "GradeUpdated" đến group của Student (group name = studentId string).
    public async Task NotifyGradeUpdatedAsync(
        Guid studentId,
        Guid courseId,
        string courseName,
        decimal grade,
        CancellationToken cancellationToken = default) {
        await _hubContext.Clients
            .Group(studentId.ToString())
            .SendAsync("GradeUpdated", new {
                CourseId = courseId,
                CourseName = courseName,
                Grade = grade
            }, cancellationToken);
    }
}

// Marker class cho IHubContext injection.
// Hub thực sự được implement ở Enrollment.Presentation.
// Đặt ở đây để Infrastructure không phụ thuộc vào Presentation assembly.
public sealed class GradeHub : Hub { }