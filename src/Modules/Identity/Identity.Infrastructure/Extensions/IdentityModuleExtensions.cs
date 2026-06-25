using System.Reflection;
using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Options;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Seed;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Identity.Infrastructure.Extensions;

// DI registration cho toàn bộ Identity Infrastructure.
// Được gọi từ TSMS.Api — Composition Root duy nhất biết về tất cả modules.
public static class IdentityModuleExtensions {
    // Assembly của Application Layer — expose để Program.cs scan MediatR handlers.
    public static readonly Assembly ApplicationAssembly =
        typeof(Identity.Application.Authentication.Login.LoginCommand).Assembly;
    
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddDbContext(configuration);  
        services.AddIdentityCore();
        services.AddJwtAuthentication(configuration);
        services.AddRepositoriesAndServices();

        return services;
    }

    // Seed roles sau khi app đã build — gọi từ Program.cs
    public static async Task SeedIdentityDataAsync(this WebApplication app) {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        await IdentityDataSeeder.SeedRolesAsync(roleManager);
    }

    // ── Private helpers 

    private static void AddDbContext(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("IdentityDb")));
    }

    private static void AddIdentityCore(this IServiceCollection services) {
        services
            .AddIdentityCore<AppUser>(options => {
                // Password policy
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
 
                options.SignIn.RequireConfirmedEmail = false;
 
                // Lockout — TimeSpan.MaxValue để deactivated user không bao giờ tự unlock
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.MaxValue;
                options.Lockout.MaxFailedAccessAttempts = int.MaxValue;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddSignInManager();
    }

    private static void AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration) {
        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()!;
 
        services.AddSingleton(jwtOptions);
 
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };
            });
 
        services.AddAuthorization();
    }

    private static void AddRepositoriesAndServices(this IServiceCollection services) {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<ITokenService, TokenService>();
    }
}