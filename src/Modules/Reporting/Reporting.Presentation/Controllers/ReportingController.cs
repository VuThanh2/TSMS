using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reporting.Application.Attendance.GetCourseAttendanceReport;
using Reporting.Application.CourseStatistics.GetCourseStatistics;
using Reporting.Application.PersonalSummary.GetMyPersonalSummary;
using Reporting.Application.ScoreDistribution.GetScoreDistribution;
using Reporting.Application.StudentGrades.GetStudentGrades;

namespace Reporting.Presentation.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportingController : ControllerBase {
    private readonly ISender _sender;

    public ReportingController(ISender sender) {
        _sender = sender;
    }

    // GET /api/reports/course-statistics
    // Admin xem thống kê tổng hợp toàn bộ Course, dùng cho Grid và 2 Bar Chart 
    [HttpGet("course-statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCourseStatistics(CancellationToken cancellationToken) {
        var result = await _sender.Send(new GetCourseStatisticsQuery(), cancellationToken);

        return Ok(result.Value);
    }

    // GET /api/reports/student-grades/{courseId}
    // Admin xem danh sách điểm số toàn bộ Student trong một Course cụ thể, dùng cho Grid 
    [HttpGet("student-grades/{courseId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStudentGrades(
        Guid courseId,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(new GetStudentGradesQuery(courseId), cancellationToken);

        if (result.IsFailure)
            return result.Error.Code == "Reporting.CourseNotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }

    // GET /api/reports/score-distribution/{courseId}
    // Admin xem phân bố điểm số Student theo nhóm xếp loại của một Course, dùng cho Pie Chart 
    [HttpGet("score-distribution/{courseId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetScoreDistribution(
        Guid courseId,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(new GetScoreDistributionQuery(courseId), cancellationToken);

        if (result.IsFailure)
            return result.Error.Code == "Reporting.CourseNotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }

    // GET /api/reports/attendance/{courseId}
    // Admin xem tất cả Course; Lecturer chỉ xem Course mình phụ trách 
    [HttpGet("attendance/{courseId:guid}")]
    [Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> GetCourseAttendanceReport(
        Guid courseId,
        CancellationToken cancellationToken) {
        // Admin bỏ qua ownership check (LecturerId = null); Lecturer phải đúng người phụ trách.
        var lecturerId = User.IsInRole("Lecturer") ? GetCurrentUserId() : null;

        if (User.IsInRole("Lecturer") && lecturerId is null) return Unauthorized();

        var result = await _sender.Send(
            new GetCourseAttendanceReportQuery(courseId, lecturerId),
            cancellationToken);

        if (result.IsFailure) {
            return result.Error.Code switch {
                "Reporting.CourseNotFound" => NotFound(new { result.Error.Code, result.Error.Message }),
                "Reporting.NotCourseOwner" => Forbid(),
                _ => BadRequest(new { result.Error.Code, result.Error.Message })
            };
        }

        return Ok(result.Value);
    }

    // GET /api/reports/my-summary
    // Student xem thống kê điểm số và điểm danh cá nhân trên toàn bộ Course đã đăng ký 
    [HttpGet("my-summary")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyPersonalSummary(CancellationToken cancellationToken) {
        var studentId = GetCurrentUserId();
        if (studentId is null) return Unauthorized();

        var result = await _sender.Send(
            new GetMyPersonalSummaryQuery(studentId.Value),
            cancellationToken);

        return Ok(result.Value);
    }

    private Guid? GetCurrentUserId() {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }
}