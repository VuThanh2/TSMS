using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EnrollmentManagement.Infrastructure.Hubs;

// Hub cho UC-33 (Real-time Grade Notification).
[Authorize]
public sealed class GradeHub : Hub {
    // Tự động add connection vào group theo userId ngay khi connect thành công,
    // để SignalRNotificationService.NotifyGradeUpdatedAsync gửi đúng Student (Clients.Group(studentId)).
    public override async Task OnConnectedAsync() {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? Context.User?.FindFirstValue("sub");

        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }
}