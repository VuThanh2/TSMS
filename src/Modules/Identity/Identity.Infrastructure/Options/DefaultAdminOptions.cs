namespace Identity.Infrastructure.Options;

// Strongly-typed config cho tài khoản Admin mặc định, đọc từ appsettings.json section "DefaultAdmin".
// Chỉ dùng lúc seed — không liên quan runtime authentication.
public class DefaultAdminOptions {
    public const string SectionName = "DefaultAdmin";

    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = "System Administrator";
}