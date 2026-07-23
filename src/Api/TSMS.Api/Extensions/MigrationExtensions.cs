using CourseManagement.Infrastructure.Persistence;
using EnrollmentManagement.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Reporting.Infrastructure.Persistence;

namespace TSMS.Api.Extensions;

// Áp dụng migration cho cả 4 DbContext ngay lúc app khởi động, TRƯỚC khi seed.
public static class MigrationExtensions {
    // API khởi động gần như tức thì, còn SQL Server container mất 30-90s mới nhận kết nối.
    // Không retry thì API luôn thua cuộc đua này và chết hẳn ở mọi lần redeploy.
    private const int MaxAttempts = 12;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    public static async Task MigrateDatabasesAsync(this WebApplication app) {
        var logger = app.Logger;
        var delay = InitialDelay;

        for (var attempt = 1; ; attempt++) {
            try {
                await ApplyMigrationsAsync(app);
                logger.LogInformation("Migration hoàn tất ở lần thử {Attempt}.", attempt);
                return;
            }
            // Chỉ nuốt lỗi hạ tầng tạm thời. Lỗi migration thật (script sai, schema xung đột)
            // phải nổ ngay để lộ ra lúc deploy, không được che bằng retry.
            catch (Exception ex) when (attempt < MaxAttempts && IsTransient(ex)) {
                logger.LogWarning(
                    "Lần {Attempt}/{Max}: chưa kết nối được database, thử lại sau {Delay}s. {Message}",
                    attempt, MaxAttempts, delay.TotalSeconds, ex.Message);

                await Task.Delay(delay);
                // Backoff luỹ tiến, chặn trần để không chờ quá lâu giữa 2 lần thử.
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, MaxDelay.TotalSeconds));
            }
        }
    }

    private static async Task ApplyMigrationsAsync(WebApplication app) {
        // Tạo scope mới cho mỗi lần thử: DbContext đã hỏng ở lần trước không dùng lại được.
        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider;

        await provider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
        await provider.GetRequiredService<CourseDbContext>().Database.MigrateAsync();
        await provider.GetRequiredService<EnrollmentDbContext>().Database.MigrateAsync();
        await provider.GetRequiredService<ReportingDbContext>().Database.MigrateAsync();
    }

    // Lỗi mạng/timeout lúc DB chưa sẵn sàng thì đáng thử lại; lỗi logic thì không.
    private static bool IsTransient(Exception ex) => ex switch {
        SqlException or TimeoutException => true,
        _ => ex.InnerException is not null && IsTransient(ex.InnerException)
    };
}
