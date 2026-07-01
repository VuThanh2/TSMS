using System.Security.Claims;
using Identity.Application.Users.CreateUser;
using Identity.Application.Users.GetUserById;
using Identity.Application.Users.GetUsers;
using Identity.Application.Users.ImportUsersCsv;
using Identity.Application.Users.UpdateUser;
using Identity.Application.Users.UpdateUserStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Presentation.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase {
    private readonly ISender _sender;

    public UsersController(ISender sender) {
        _sender = sender;
    }

    // GET /api/users?page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        var result = await _sender.Send(
            new GetUsersQuery(page, pageSize), cancellationToken);

        return Ok(result.Value);
    }

    // GET /api/users/{userId}
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserById(
        Guid userId,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(new GetUserByIdQuery(userId), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }

    // POST /api/users
    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserInputDto dto,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new CreateUserCommand(dto.FullName, dto.Email, dto.Role, dto.Password),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { result.Error.Code, result.Error.Message });

        return CreatedAtAction(
            nameof(GetUserById),
            new { userId = result.Value.UserId },
            result.Value);
    }
    
    // POST /api/users/import-csv
    [HttpPost("import-csv")]
    public async Task<IActionResult> ImportCsv(
        [FromForm] ImportUsersCsvInputDto dto,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new ImportUsersCsvCommand(dto.File), cancellationToken);
 
        if (result.IsFailure)
            return BadRequest(new { result.Error.Code, result.Error.Message });
 
        return Ok(result.Value);
    }
 
    // PUT /api/users/{userId}
    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> UpdateUser(
        Guid userId,
        [FromBody] UpdateUserInputDto dto,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new UpdateUserCommand(userId, dto.FullName, dto.Email, dto.Department, dto.Major),
            cancellationToken);
 
        if (result.IsFailure)
            return result.Error.Code == "User.NotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });
 
        return Ok(result.Value);
    }
 
    // PUT /api/users/{userId}/status
    [HttpPut("{userId:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        Guid userId,
        [FromBody] UpdateUserStatusInputDto dto,
        CancellationToken cancellationToken) {
        // currentUserId resolve từ JWT claim — dùng để check CannotDeactivateSelf.
        var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
 
        if (currentUserIdClaim is null || !Guid.TryParse(currentUserIdClaim, out var currentUserId))
            return Unauthorized();
 
        var result = await _sender.Send(
            new UpdateUserStatusCommand(userId, dto.IsActive, currentUserId),
            cancellationToken);
 
        if (result.IsFailure)
            return result.Error.Code == "User.NotFound"
                ? NotFound(new { result.Error.Code, result.Error.Message })
                : BadRequest(new { result.Error.Code, result.Error.Message });
 
        return Ok(result.Value);
    }
}