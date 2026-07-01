namespace Identity.Infrastructure.Options;

// Strongly-typed config cho JWT settings từ appsettings.json section "Jwt".
public class JwtOptions {
    public const string SectionName = "Jwt";

    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; } = 60;
}