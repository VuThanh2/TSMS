using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reporting.Infrastructure.Persistence;

// Chỉ dùng cho EF Core design-time tooling (dotnet ef migrations add).
// Không được đăng ký vào DI container.
public class ReportingDbContextFactory : IDesignTimeDbContextFactory<ReportingDbContext> {
    public ReportingDbContext CreateDbContext(string[] args) {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=TSMS_Reporting;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True")
            .Options;

        return new ReportingDbContext(options);
    }
}