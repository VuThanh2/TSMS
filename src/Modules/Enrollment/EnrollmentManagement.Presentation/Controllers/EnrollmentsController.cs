using System.Security.Claims;
using EnrollmentManagement.Application.Enrollments.AdjustSession;
using EnrollmentManagement.Application.Enrollments.EnrollCourse;
using EnrollmentManagement.Application.Enrollments.GetMyEnrollments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnrollmentManagement.Presentation.Controllers;

[ApiController]
[Route("api/enrollments")]
[Authorize]
public class EnrollmentsController : ControllerBase {
    private readonly ISender _sender;

    public EnrollmentsController(ISender sender) {
        _sender = sender;
    }

    // GET /api/enrollments/my-courses?page=&pageSize=
    // Student xem danh sách toàn bộ Course đã đăng ký kèm điểm số.
    [HttpGet("my-courses")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyEnrollments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        var studentId = GetCurrentUserId();
        if (studentId is null) return Unauthorized();

        var result = await _sender.Send(
            new GetMyEnrollmentsQuery(studentId.Value, page, pageSize),
            cancellationToken);

        return Ok(result.Value);
    }

    // POST /api/enrollments
    // Student đăng ký một Course và chọn đúng 2 WeeklySlot (khung giờ lặp lại hàng tuần
    // cho cả kỳ, không phải 2 buổi học cụ thể).
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> EnrollCourse(
        [FromBody] EnrollCourseInputDto dto,
        CancellationToken cancellationToken) {
        var studentId = GetCurrentUserId();
        if (studentId is null) return Unauthorized();

        var result = await _sender.Send(
            new EnrollCourseCommand(studentId.Value, dto.CourseId, dto.WeeklySlotIds),
            cancellationToken);

        if (result.IsFailure) {
            return result.Error.Code switch {
                "Enrollment.CourseNotEnrollable" => BadRequest(new { result.Error.Code, result.Error.Message }),
                "Enrollment.CourseIsFull" => BadRequest(new { result.Error.Code, result.Error.Message }),
                "Enrollment.AlreadyEnrolled" => Conflict(new { result.Error.Code, result.Error.Message }),
                _ => BadRequest(new { result.Error.Code, result.Error.Message })
            };
        }

        return CreatedAtAction(nameof(GetMyEnrollments), result.Value);
    }

    // PUT /api/enrollments/{enrollmentId}/sessions
    // Student điều chỉnh lại 1 WeeklySlot đã chọn trong một Course đã đăng ký.
    [HttpPut("{enrollmentId:guid}/sessions")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> AdjustSession(
        Guid enrollmentId,
        [FromBody] AdjustSessionInputDto dto,
        CancellationToken cancellationToken) {
        var studentId = GetCurrentUserId();
        if (studentId is null) return Unauthorized();

        var result = await _sender.Send(
            new AdjustSessionCommand(
                enrollmentId,
                studentId.Value,
                dto.OldWeeklySlotId,
                dto.NewWeeklySlotId),
            cancellationToken);

        if (result.IsFailure) {
            return result.Error.Code switch {
                "Enrollment.NotFound" => NotFound(new { result.Error.Code, result.Error.Message }),
                "Enrollment.CourseAlreadyCompleted" => BadRequest(new { result.Error.Code, result.Error.Message }),
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