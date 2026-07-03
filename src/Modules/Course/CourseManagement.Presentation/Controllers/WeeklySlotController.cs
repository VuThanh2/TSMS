using CourseManagement.Application.WeeklySlots.AddWeeklySlot;
using CourseManagement.Application.WeeklySlots.RemoveWeeklySlot;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Presentation.Controllers;

[ApiController]
[Route("api/courses/{courseId:guid}/weekly-slots")]
[Authorize]
public class WeeklySlotsController : ControllerBase {
    private readonly ISender _sender;

    public WeeklySlotsController(ISender sender) {
        _sender = sender;
    }

    // POST /api/courses/{courseId}/weekly-slots
    // Admin thêm 1 khung giờ lặp lại hàng tuần — hệ thống tự sinh toàn bộ ClassSession
    // từ StartDate đến EndDate của Course.
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddWeeklySlot(
        Guid courseId,
        [FromBody] AddWeeklySlotInputDto dto,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new AddWeeklySlotCommand(courseId, dto.DayOfWeek, dto.SessionType),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Code == "Course.NotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });

        return Created(string.Empty, result.Value);
    }

    // DELETE /api/courses/{courseId}/weekly-slots/{weeklySlotId}
    // Admin xóa 1 khung giờ — chỉ hủy các ClassSession TƯƠNG LAI, buổi đã qua giữ nguyên.
    // Từ chối nếu còn Student đang enroll vào slot này (cross-BC check).
    [HttpDelete("{weeklySlotId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveWeeklySlot(
        Guid courseId,
        Guid weeklySlotId,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new RemoveWeeklySlotCommand(courseId, weeklySlotId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Code switch {
                "Course.NotFound" => NotFound(new { result.Error.Code, result.Error.Message }),
                "Course.WeeklySlotInUse" => Conflict(new { result.Error.Code, result.Error.Message }),
                _ => BadRequest(new { result.Error.Code, result.Error.Message })
            };

        return NoContent();
    }
}