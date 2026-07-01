using System.Security.Claims;
using EnrollmentManagement.Application.Schedules.GetLecturerSchedule;
using EnrollmentManagement.Application.Schedules.GetStudentSchedule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnrollmentManagement.Presentation.Controllers;

[ApiController]
[Route("api/schedule")]
[Authorize]
public class ScheduleController : ControllerBase {
    private readonly ISender _sender;

    public ScheduleController(ISender sender) {
        _sender = sender;
    }

    // GET /api/schedule/lecturer
    [HttpGet("lecturer")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> GetLecturerSchedule(CancellationToken cancellationToken) {
        var lecturerId = GetCurrentUserId();
        if (lecturerId is null) return Unauthorized();

        var result = await _sender.Send(
            new GetLecturerScheduleQuery(lecturerId.Value),
            cancellationToken);

        return Ok(result.Value);
    }

    // GET /api/schedule/student
    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentSchedule(CancellationToken cancellationToken) {
        var studentId = GetCurrentUserId();
        if (studentId is null) return Unauthorized();

        var result = await _sender.Send(
            new GetStudentScheduleQuery(studentId.Value),
            cancellationToken);

        return Ok(result.Value);
    }

    private Guid? GetCurrentUserId() {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }
}