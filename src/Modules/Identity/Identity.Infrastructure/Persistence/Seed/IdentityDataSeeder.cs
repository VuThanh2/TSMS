using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Persistence.Seed;

// Seed 3 rows cố định vào AspNetRoles, và 1 tài khoản Admin mặc định khi ứng dụng khởi động.
public static class IdentityDataSeeder {
    public static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager) {
        var roles = Enum.GetNames<UserRole>(); // ["Admin", "Lecturer", "Student"]
 
        foreach (var role in roles) {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
 
    // Tạo tài khoản Admin mặc định nếu chưa tồn tại
    // Idempotent: gọi lại nhiều lần (mỗi lần app start) không tạo trùng, không lỗi.
    public static async Task SeedAdminAsync(
        UserManager<AppUser> userManager,
        DefaultAdminOptions options,
        ILogger logger) {
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password)) {
            logger.LogWarning("DefaultAdmin chưa được cấu hình trong appsettings.json — bỏ qua seed Admin.");
            return;
        }
 
        var existingAdmin = await userManager.FindByEmailAsync(options.Email);
        if (existingAdmin is not null)
            return;
 
        var createResult = AppUser.Create(
            id: Guid.NewGuid(),
            email: options.Email,
            fullName: options.FullName,
            role: UserRole.Admin);
 
        if (createResult.IsFailure) {
            logger.LogError("Seed Admin thất bại ở bước tạo Domain entity: {Error}", createResult.Error.Message);
            return;
        }
 
        var identityResult = await userManager.CreateAsync(createResult.Value, options.Password);
 
        if (!identityResult.Succeeded) {
            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            logger.LogError("Seed Admin thất bại ở bước UserManager.CreateAsync: {Errors}", errors);
            return;
        }
 
        await userManager.AddToRoleAsync(createResult.Value, nameof(UserRole.Admin));
 
        logger.LogInformation("Đã seed tài khoản Admin mặc định: {Email}", options.Email);
    }
}