using System.Security.Claims;
using CourseManagement.Application.Courses.CreateCourse;
using CourseManagement.Application.Courses.DeleteCourse;
using CourseManagement.Application.Courses.GetAvailableCourses;
using CourseManagement.Application.Courses.GetCourseById;
using CourseManagement.Application.Courses.GetCourses;
using CourseManagement.Application.Courses.OpenCourseEnrollment;
using CourseManagement.Application.Courses.ReplaceLecturer;
using CourseManagement.Application.Courses.UpdateCourse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Presentation.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CourseController : ControllerBase {
    private readonly ISender _sender;

    public CourseController(ISender sender) {
        _sender = sender;
    }

    // GET /api/courses?page=&pageSize=&keyword=&status=
    // Chỉ Admin. Trả về toàn bộ Course, hỗ trợ search theo tên và filter theo status.
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCourses(
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        var result = await _sender.Send(
            new GetCoursesQuery(keyword, status, LecturerId: null, page, pageSize),
            cancellationToken);
 
        return Ok(result.Value);
    }
    
    // GET /api/courses/my-courses?page=&pageSize=&keyword=&status=
    // Chỉ Lecturer. Lọc tự động theo lecturerId từ token, hỗ trợ search + filter status.
    [HttpGet("my-courses")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> GetMyLecturingCourses(
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");
 
        if (claim is null || !Guid.TryParse(claim, out var lecturerId))
            return Unauthorized();
 
        var result = await _sender.Send(
            new GetCoursesQuery(keyword, status, lecturerId, page, pageSize),
            cancellationToken);
 
        return Ok(result.Value);
    }

    // GET /api/courses/available?page=&pageSize=
    // Student xem danh sách Upcoming courses chưa đăng ký.
    [HttpGet("available")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetAvailableCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (claim is null || !Guid.TryParse(claim, out var studentId))
            return Unauthorized();

        var result = await _sender.Send(
            new GetAvailableCoursesQuery(studentId, page, pageSize),
            cancellationToken);

        return Ok(result.Value);
    }

    // GET /api/courses/{courseId}
    // Tất cả roles. Lecturer chỉ được xem course của mình.
    [HttpGet("{courseId:guid}")]
    [Authorize(Roles = "Admin,Lecturer,Student")]
    public async Task<IActionResult> GetCourseById(
        Guid courseId,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(new GetCourseByIdQuery(courseId), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { result.Error.Code, result.Error.Message });

        // Lecturer chỉ được xem course của chính mình.
        if (User.IsInRole("Lecturer")) {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (claim is null || !Guid.TryParse(claim, out var lecturerId))
                return Unauthorized();

            if (result.Value.LecturerId != lecturerId)
                return Forbid();
        }

        return Ok(result.Value);
    }

    // POST /api/courses
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCourse(
        [FromBody] CreateCourseInputDto dto,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new CreateCourseCommand(
                dto.LecturerId,
                dto.Name,
                dto.Description,
                dto.StartDate,
                dto.EndDate,
                dto.MaxCapacity),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { result.Error.Code, result.Error.Message });

        return CreatedAtAction(
            nameof(GetCourseById),
            new { courseId = result.Value.CourseId },
            result.Value);
    }

    // PUT /api/courses/{courseId}
    [HttpPut("{courseId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCourse(
        Guid courseId,
        [FromBody] UpdateCourseInputDto dto,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new UpdateCourseCommand(courseId, dto.Name, dto.Description, dto.EndDate, dto.MaxCapacity),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Code == "Course.NotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }

    // DELETE /api/courses/{courseId}
    // Chỉ Admin. Ràng buộc: course phải Upcoming (chưa bắt đầu) và CHƯA có Student nào enroll.
    // Dùng cho case Admin tạo nhầm/dư — xóa thật (cascade WeeklySlot + ClassSession).
    [HttpDelete("{courseId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCourse(
        Guid courseId,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(new DeleteCourseCommand(courseId), cancellationToken);

        if (result.IsFailure)
            return result.Error.Code == "Course.NotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }

    // PUT /api/courses/open-enrollment/{courseId}
    // Chỉ Admin. Mở cổng cho Student đăng ký — trước đó Course vô hình với Student, Admin còn
    // sửa/xóa được. Ràng buộc: course phải Upcoming và đã có tối thiểu 2 WeeklySlot.
    [HttpPut("open-enrollment/{courseId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> OpenCourseEnrollment(
        Guid courseId,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(new OpenCourseEnrollmentCommand(courseId), cancellationToken);

        if (result.IsFailure)
            return result.Error.Code == "Course.NotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }

    // PUT /api/courses/lecturer/{courseId}
    [HttpPut("lecturer/{courseId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReplaceLecturer(
        Guid courseId,
        [FromBody] ReplaceLecturerInputDto dto,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new ReplaceLecturerCommand(courseId, dto.NewLecturerId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Code == "Course.NotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }
}