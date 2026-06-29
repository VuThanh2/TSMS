using System.Security.Claims;
using EnrollmentManagement.Application.Enrollments.GetCourseEnrollments;
using EnrollmentManagement.Application.Grading.GradeStudent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnrollmentManagement.Presentation.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class GradingController : ControllerBase {
    private readonly ISender _sender;

    public GradingController(ISender sender) {
        _sender = sender;
    }

    // GET /api/courses/{courseId}/enrollments?page=&pageSize=
    // Lecturer xem danh sách Student đã enroll trong Course kèm điểm số.
    [HttpGet("courses/{courseId:guid}/enrollments")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> GetCourseEnrollments(
        Guid courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        var lecturerId = GetCurrentUserId();
        if (lecturerId is null) return Unauthorized();
 
        var result = await _sender.Send(
            new GetCourseEnrollmentsQuery(courseId, lecturerId.Value, page, pageSize),
            cancellationToken);
 
        if (result.IsFailure)
            return result.Error.Code == "Enrollment.NotCourseOwner"
                ? Forbid()
                : BadRequest(new { result.Error.Code, result.Error.Message });
 
        return Ok(result.Value);
    }

    // PUT /api/enrollments/{enrollmentId}/grade
    [HttpPut("enrollments/{enrollmentId:guid}/grade")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> GradeStudent(
        Guid enrollmentId,
        [FromBody] GradeStudentInputDto dto,
        CancellationToken cancellationToken) {
        var lecturerId = GetCurrentUserId();
        if (lecturerId is null) return Unauthorized();

        var result = await _sender.Send(
            new GradeStudentCommand(enrollmentId, lecturerId.Value, dto.Grade),
            cancellationToken);

        if (result.IsFailure) {
            return result.Error.Code switch {
                "Enrollment.NotFound" => NotFound(new { result.Error.Code, result.Error.Message }),
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