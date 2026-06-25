using Identity.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Persistence.Seed;

// Seed 3 rows cố định vào AspNetRoles khi ứng dụng khởi động.
public static class IdentityDataSeeder {
    public static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager) {
        var roles = Enum.GetNames<UserRole>(); // ["Admin", "Lecturer", "Student"]

        foreach (var role in roles) {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}