using CourseManagement.Application.ClassSessions.DeleteClassSession;
using CourseManagement.Application.ClassSessions.GetClassSessions;
using CourseManagement.Application.ClassSessions.UpdateClassSession;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Presentation.Controllers;

// Tạo buổi học mới đã chuyển sang WeeklySlotsController (POST /weekly-slots) — 1 lần thêm
// sinh hàng loạt ClassSession cho cả kỳ, thay vì tạo từng buổi rời rạc như trước.
// Controller này chỉ còn thao tác trên buổi học ĐÃ TỒN TẠI: xem, dời ngày 1 buổi cụ thể
// (vd nghỉ lễ), hoặc hủy 1 buổi cụ thể mà không ảnh hưởng cả WeeklySlot.
[ApiController]
[Route("api/courses/{courseId:guid}/sessions")]
[Authorize]
public class ClassSessionsController : ControllerBase {
    private readonly ISender _sender;

    public ClassSessionsController(ISender sender) {
        _sender = sender;
    }

    // GET /api/courses/{courseId}/sessions
    [HttpGet]
    [Authorize(Roles = "Admin,Lecturer,Student")]
    public async Task<IActionResult> GetClassSessions(
        Guid courseId,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new GetClassSessionsQuery(courseId), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }

    // PUT /api/courses/{courseId}/sessions/{sessionId}
    // Dời ngày 1 buổi cụ thể (vd nghỉ lễ) — không ảnh hưởng WeeklySlot gốc.
    [HttpPut("{sessionId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateClassSession(
        Guid courseId,
        Guid sessionId,
        [FromBody] UpdateClassSessionInputDto dto,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new UpdateClassSessionCommand(courseId, sessionId, dto.SessionDate, dto.SessionType),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Code == "Course.NotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }

    // DELETE /api/courses/{courseId}/sessions/{sessionId}
    // Hủy 1 buổi cụ thể (vd nghỉ lễ) — các tuần khác cùng WeeklySlot vẫn diễn ra bình thường.
    // Muốn hủy cả khung giờ lặp lại, dùng DELETE /weekly-slots/{weeklySlotId} thay vì API này.
    [HttpDelete("{sessionId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteClassSession(
        Guid courseId,
        Guid sessionId,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new DeleteClassSessionCommand(courseId, sessionId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Code == "Course.NotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });

        return NoContent();
    }
}