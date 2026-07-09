namespace TSMS.Api.Options;

// Strongly-typed config cho CORS settings từ appsettings.json section "Cors".
// Theo cùng pattern với JwtOptions/DefaultAdminOptions (Identity.Infrastructure.Options) —
// tránh đọc config bằng magic string rải rác ở nhiều nơi.
public class CorsOptions {
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}