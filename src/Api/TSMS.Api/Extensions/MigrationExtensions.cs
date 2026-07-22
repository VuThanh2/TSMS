using CourseManagement.Infrastructure.Persistence;
using EnrollmentManagement.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Reporting.Infrastructure.Persistence;

namespace TSMS.Api.Extensions;

// Áp dụng migration cho cả 4 DbContext ngay lúc app khởi động, TRƯỚC khi seed.
public static class MigrationExtensions {
    public static async Task MigrateDatabasesAsync(this WebApplication app) {
        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider;

        await provider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
        await provider.GetRequiredService<CourseDbContext>().Database.MigrateAsync();
        await provider.GetRequiredService<EnrollmentDbContext>().Database.MigrateAsync();
        await provider.GetRequiredService<ReportingDbContext>().Database.MigrateAsync();
    }
}
