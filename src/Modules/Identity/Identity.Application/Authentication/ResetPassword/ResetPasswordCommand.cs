using Identity.Domain.Errors;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SharedKernel.Primitives;

namespace Identity.Application.Authentication.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string NewPassword) : IRequest<Result>;

// Xác nhận email tồn tại + active → đặt lại mật khẩu trực tiếp.
public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result> {
    private readonly UserManager<Domain.Entities.AppUser> _userManager;

    public ResetPasswordCommandHandler(UserManager<Domain.Entities.AppUser> userManager) {
        _userManager = userManager;
    }

    public async Task<Result> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken) {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        // UC-04 Business Rule: tài khoản inactive không thể reset password.
        if (!user.IsActive)
            return Result.Failure(UserErrors.AccountIsInactive);

        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
            return Result.Failure(UserErrors.PasswordResetFailed);

        var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!addResult.Succeeded) {
            var description = addResult.Errors.FirstOrDefault()?.Description
                              ?? "Mật khẩu không đáp ứng yêu cầu bảo mật.";
            return Result.Failure(Error.Create("User.PasswordPolicyViolation", description));
        }

        return Result.Success();
    }
}