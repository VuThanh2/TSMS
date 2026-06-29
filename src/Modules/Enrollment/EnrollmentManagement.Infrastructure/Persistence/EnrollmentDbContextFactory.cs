using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EnrollmentManagement.Infrastructure.Persistence;

// Design-time factory cho EF Core CLI (dotnet ef migrations add / database update).
// Chỉ dùng bởi tooling — không đăng ký trong DI.
public class EnrollmentDbContextFactory : IDesignTimeDbContextFactory<EnrollmentDbContext> {
    public EnrollmentDbContext CreateDbContext(string[] args) {
        var options = new DbContextOptionsBuilder<EnrollmentDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=TSMS_Enrollment;User Id=sa;Password=Tsms2026@@;TrustServerCertificate=True;")
            .Options;

        return new EnrollmentDbContext(options);
    }
}