using System.Security.Claims;
using EnrollmentManagement.Application.Attendances.GetCourseAttendanceSummary;
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

    // GET /api/sessions/attendances/{sessionId}
    // Lecturer xem danh sách điểm danh của tất cả Student trong một ca học.
    // KHÔNG nhận courseId từ client: Course sở hữu ca học được suy ra từ chính sessionId ở
    // handler rồi mới check ownership — nhận courseId rời sẽ cho phép Lecturer ghép courseId
    // mình sở hữu với sessionId của Course khác để đọc trộm điểm danh.
    [HttpGet("sessions/attendances/{sessionId:guid}")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> GetSessionAttendances(
        Guid sessionId,
        CancellationToken cancellationToken) {
        var lecturerId = GetCurrentUserId();
        if (lecturerId is null) return Unauthorized();

        var result = await _sender.Send(
            new GetSessionAttendancesQuery(sessionId, lecturerId.Value),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Code switch {
                "Enrollment.ClassSessionNotFound" => NotFound(new { result.Error.Code, result.Error.Message }),
                "Enrollment.NotCourseOwner" => Forbid(),
                _ => BadRequest(new { result.Error.Code, result.Error.Message })
            };

        return Ok(result.Value);
    }

    // GET /api/courses/attendance-summary/{courseId}
    // Lecturer xem số liệu điểm danh của từng buổi trong Course — để lưới lịch tuần
    // hiển thị số có mặt thay vì chỉ "Past".
    [HttpGet("courses/attendance-summary/{courseId:guid}")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> GetCourseAttendanceSummary(
        Guid courseId,
        CancellationToken cancellationToken) {
        var lecturerId = GetCurrentUserId();
        if (lecturerId is null) return Unauthorized();

        var result = await _sender.Send(
            new GetCourseAttendanceSummaryQuery(courseId, lecturerId.Value),
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