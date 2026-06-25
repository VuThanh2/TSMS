namespace Identity.Application.Authentication.ResetPassword;

public sealed record ResetPasswordInputDto(string Email, string NewPassword);