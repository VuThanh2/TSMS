using CourseManagement.Application.ClassSessions.CancelClassSession;
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
[Route("api/courses/sessions")]
[Authorize]
public class ClassSessionsController : ControllerBase {
    private readonly ISender _sender;

    public ClassSessionsController(ISender sender) {
        _sender = sender;
    }

    // GET /api/courses/sessions/{courseId}
    [HttpGet("{courseId:guid}")]
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

    // PUT /api/courses/sessions/{courseId}/{sessionId}
    // Dời ngày 1 buổi cụ thể (vd nghỉ lễ) — không ảnh hưởng WeeklySlot gốc.
    [HttpPut("{courseId:guid}/{sessionId:guid}")]
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
    
    // Giữ nguyên verb DELETE cho quen thuộc REST (loại buổi này khỏi lịch hoạt động), nhưng bên
    // trong là soft-cancel (IsCancelled = true) — KHÔNG xóa vật lý, vì Attendance có thể đã
    // pre-populate sẵn tham chiếu tới buổi này. Muốn hủy cả khung giờ lặp lại, dùng
    // DELETE /api/courses/weekly-slots/{courseId}/{weeklySlotId} thay vì API này.
    [HttpDelete("{courseId:guid}/{sessionId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CancelClassSession(
        Guid courseId,
        Guid sessionId,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new CancelClassSessionCommand(courseId, sessionId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Code == "Course.NotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });

        return NoContent();
    }
}