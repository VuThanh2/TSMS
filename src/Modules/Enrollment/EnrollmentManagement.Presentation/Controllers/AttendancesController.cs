using System.Security.Claims;
using EnrollmentManagement.Application.Attendances.GetSessionAttendances;
using EnrollmentManagement.Application.Attendances.MarkAttendance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnrollmentManagement.Presentation.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class AttendancesController : ControllerBase {
    private readonly ISender _sender;

    public AttendancesController(ISender sender) {
        _sender = sender;
    }

    // GET /api/courses/{courseId}/sessions/{sessionId}/attendances
    // Lecturer xem danh sách điểm danh của tất cả Student trong một ca học.
    [HttpGet("courses/{courseId:guid}/sessions/{sessionId:guid}/attendances")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> GetSessionAttendances(
        Guid courseId,
        Guid sessionId,
        CancellationToken cancellationToken) {
        var lecturerId = GetCurrentUserId();
        if (lecturerId is null) return Unauthorized();

        var result = await _sender.Send(
            new GetSessionAttendancesQuery(courseId, sessionId, lecturerId.Value),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Code == "Enrollment.NotCourseOwner"
                ? Forbid()
                : BadRequest(new { result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }

    // PUT /api/attendances/{attendanceId}
    // Lecturer cập nhật trạng thái điểm danh cho một Student trong một ca học.
    [HttpPut("attendances/{attendanceId:guid}")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> MarkAttendance(
        Guid attendanceId,
        [FromBody] MarkAttendanceInputDto dto,
        CancellationToken cancellationToken) {
        var lecturerId = GetCurrentUserId();
        if (lecturerId is null) return Unauthorized();

        var result = await _sender.Send(
            new MarkAttendanceCommand(attendanceId, lecturerId.Value, dto.AttendanceStatus),
            cancellationToken);

        if (result.IsFailure) {
            return result.Error.Code switch {
                "Enrollment.AttendanceNotFound" => NotFound(new { result.Error.Code, result.Error.Message }),
                "Enrollment.NotCourseOwner" => Forbid(),
                _ => BadRequest(new { result.Error.Code, result.Error.Message })
            };
        }

        return Ok(result.Value);
    }

    private Guid? GetCurrentUserId() {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }
}