using Identity.Application.Authentication.Login;
using Identity.Application.Authentication.Logout;
using Identity.Application.Authentication.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase {
    private readonly ISender _sender;

    public AuthenticationController(ISender sender) {
        _sender = sender;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginInputDto dto,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new LoginCommand(dto.Email, dto.Password), cancellationToken);

        if (result.IsFailure)
            return Unauthorized(new { result.Error.Code, result.Error.Message });

        return Ok(new { accessToken = result.Value.AccessToken });
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken) {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _sender.Send(new LogoutCommand(userId), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { result.Error.Code, result.Error.Message });

        return NoContent();
    }

    // POST /api/auth/reset-password
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordInputDto dto,
        CancellationToken cancellationToken) {
        var result = await _sender.Send(
            new ResetPasswordCommand(dto.Email, dto.NewPassword), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { result.Error.Code, result.Error.Message });

        return NoContent();
    }
}