using CourseManagement.Infrastructure.Persistence;
using EnrollmentManagement.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Reporting.Infrastructure.Persistence;
using SharedInfrastructure.Persistence;

namespace TSMS.DbMigrator;

// Console tool độc lập — chỉ chịu trách nhiệm áp dụng migration cho cả 4 DbContext.
// Chạy thủ công: dotnet run --project src/Migrator/TSMS.DbMigrator
public static class Program {
    public static int Main(string[] args) {
        var configuration = BuildConfiguration();
        var results = new List<(string ContextName, bool Success, string? Error)>();

        Console.WriteLine("=== TSMS Database Migrator ===");

        results.Add(MigrateContext("IdentityDb", connectionString => {
            var options = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseTsmsSqlServer(connectionString, TsmsSchemas.Identity)
                .Options;
            using var context = new IdentityDbContext(options);
            context.Database.Migrate();
        }, configuration));

        results.Add(MigrateContext("CourseDb", connectionString => {
            var options = new DbContextOptionsBuilder<CourseDbContext>()
                .UseTsmsSqlServer(connectionString, TsmsSchemas.Course)
                .Options;
            using var context = new CourseDbContext(options);
            context.Database.Migrate();
        }, configuration));

        results.Add(MigrateContext("EnrollmentDb", connectionString => {
            var options = new DbContextOptionsBuilder<EnrollmentDbContext>()
                .UseTsmsSqlServer(connectionString, TsmsSchemas.Enrollment)
                .Options;
            using var context = new EnrollmentDbContext(options);
            context.Database.Migrate();
        }, configuration));

        results.Add(MigrateContext("ReportingDb", connectionString => {
            var options = new DbContextOptionsBuilder<ReportingDbContext>()
                .UseTsmsSqlServer(connectionString, TsmsSchemas.Reporting)
                .Options;
            using var context = new ReportingDbContext(options);
            context.Database.Migrate();
        }, configuration));

        return PrintSummaryAndGetExitCode(results);
    }

    // Đọc appsettings.json / appsettings.Development.json của TSMS.Api để dùng chung một nguồn
    // connection string duy nhất, tránh lặp lại cấu hình như từng xảy ra ở các DbContextFactory.
    //
    // Cả 2 file JSON đều optional: true — vì path tương đối ngược 5 cấp thư mục này chỉ đúng khi
    // chạy `dotnet run` tại local.
    // AddEnvironmentVariables() đặt SAU CÙNG để override JSON khi cần
    private static IConfiguration BuildConfiguration() {
        var apiProjectPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Api", "TSMS.Api");

        return new ConfigurationBuilder()
            .SetBasePath(Path.GetFullPath(apiProjectPath))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static (string ContextName, bool Success, string? Error) MigrateContext(
        string connectionStringKey,
        Action<string> migrateAction,
        IConfiguration configuration) {
        Console.WriteLine($"-> Migrating {connectionStringKey}...");

        try {
            var connectionString = configuration.GetConnectionString(connectionStringKey);

            if (string.IsNullOrWhiteSpace(connectionString)) {
                throw new InvalidOperationException($"Connection string '{connectionStringKey}' not found.");
            }

            migrateAction(connectionString);

            Console.WriteLine($"   [OK] {connectionStringKey} migrated successfully.");
            return (connectionStringKey, true, null);
        }
        catch (Exception ex) {
            Console.WriteLine($"   [FAIL] {connectionStringKey}: {ex.Message}");
            return (connectionStringKey, false, ex.Message);
        }
    }

    private static int PrintSummaryAndGetExitCode(List<(string ContextName, bool Success, string? Error)> results) {
        Console.WriteLine();
        Console.WriteLine("=== Summary ===");

        foreach (var result in results) {
            var status = result.Success ? "OK" : "FAILED";
            Console.WriteLine($"   {result.ContextName,-15} : {status}");
        }

        var hasFailure = results.Any(r => !r.Success);

        if (hasFailure) {
            Console.WriteLine();
            Console.WriteLine("One or more migrations failed. See details above.");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("All migrations applied successfully.");
        return 0;
    }
}