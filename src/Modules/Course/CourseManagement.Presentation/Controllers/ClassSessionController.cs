using CourseManagement.Application.ClassSessions.AddClassSession;
using CourseManagement.Application.ClassSessions.DeleteClassSession;
using CourseManagement.Application.ClassSessions.GetClassSessions;
using CourseManagement.Application.ClassSessions.UpdateClassSession;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagement.Presentation.Controllers;

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

    // POST /api/courses/{courseId}/sessions
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddClassSession(
        Guid courseId,
        [FromBody] AddClassSessionInputDto dto,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new AddClassSessionCommand(courseId, dto.SessionDate, dto.SessionType),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Code == "Course.NotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });

        return Created(string.Empty, result.Value);
    }

    // PUT /api/courses/{courseId}/sessions/{sessionId}
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