namespace TSMS.Api.Options;

// Strongly-typed config cho tính năng Demo Data Reset — đọc từ section "Demo".
// Guard lớp 2 cho POST /api/dev/reset-demo-data (lớp 1 là [Authorize(Roles="Admin")]).
// Mặc định false ở appsettings.json gốc — CHỈ bật true ở appsettings.Development.json (Local)
// hoặc qua biến môi trường Demo__EnableReset=true (Railway, riêng bản deploy demo) — tránh lọt
// endpoint xóa dữ liệu hàng loạt này vào 1 bản deploy production thật sau này.
public class DemoOptions {
    public const string SectionName = "Demo";

    public bool EnableReset { get; init; } = false;
}