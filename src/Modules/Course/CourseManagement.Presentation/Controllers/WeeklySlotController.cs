using CourseManagement.Application.WeeklySlots.AddWeeklySlot;
using CourseManagement.Application.WeeklySlots.GetWeeklySlots;
using CourseManagement.Application.WeeklySlots.RemoveWeeklySlot;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Presentation.Controllers;

[ApiController]
[Route("api/courses/weekly-slots")]
[Authorize]
public class WeeklySlotsController : ControllerBase {
    private readonly ISender _sender;

    public WeeklySlotsController(ISender sender) {
        _sender = sender;
    }

    // GET /api/courses/weekly-slots/{courseId}
    // Trả về đúng granularity "khung giờ lặp lại hàng tuần" (2-vài item)
    [HttpGet("{courseId:guid}")]
    [Authorize(Roles = "Admin,Lecturer,Student")]
    public async Task<IActionResult> GetWeeklySlots(
        Guid courseId,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new GetWeeklySlotsQuery(courseId), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }

    // POST /api/courses/weekly-slots/{courseId}
    // Admin thêm 1 khung giờ lặp lại hàng tuần — hệ thống tự sinh toàn bộ ClassSession
    // từ StartDate đến EndDate của Course.
    [HttpPost("{courseId:guid}")]
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

    // DELETE /api/courses/weekly-slots/{courseId}/{weeklySlotId}
    // Admin xóa 1 khung giờ — chỉ xóa các ClassSession TƯƠNG LAI, buổi đã qua giữ nguyên.
    // Từ chối nếu còn Student đang enroll vào slot này (cross-BC check).
    [HttpDelete("{courseId:guid}/{weeklySlotId:guid}")]
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