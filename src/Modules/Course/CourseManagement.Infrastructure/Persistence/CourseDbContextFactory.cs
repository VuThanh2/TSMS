using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CourseManagement.Infrastructure.Persistence;

/// Design-time factory for EF Core CLI tools (dotnet ef migrations add / database update).
/// Only used by tooling — not registered in DI.
public class CourseDbContextFactory : IDesignTimeDbContextFactory<CourseDbContext> {
    public CourseDbContext CreateDbContext(string[] args) {
        var options = new DbContextOptionsBuilder<CourseDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=TSMS_Course;User Id=sa;Password=Tsms2026@@;TrustServerCertificate=True;")
            .Options;

        return new CourseDbContext(options);
    }
}