using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Identity.Infrastructure.Persistence;

// Chỉ dùng lúc design-time (dotnet ef migrations/database update).
// EF Tools gọi factory này thay vì DI container khi không resolve được DbContext.
public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext> {
    public IdentityDbContext CreateDbContext(string[] args) {
        // Đọc config từ TSMS.Api — nơi chứa appsettings.json và appsettings.Development.json.
        var basePath = Path.Combine(Directory.GetCurrentDirectory());
 
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("IdentityDb");

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new IdentityDbContext(optionsBuilder.Options);
    }
}