using FluentValidation;
using Identity.Infrastructure.Extensions;
using Identity.Presentation.Controllers;
using MediatR;

namespace TSMS.Api.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddControllers().AddApplicationPart(typeof(AuthenticationController).Assembly);
        services.AddCorsPolicy(configuration);
        services.AddMediatRWithValidation();

        return services;
    }

    // ── Private helpers
    private static void AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration) {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options => {
            options.AddPolicy("AllowFrontend", policy => {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    private static void AddMediatRWithValidation(this IServiceCollection services) {
        // Scan tất cả Application assemblies của các module tại Composition Root.
        // Mỗi module expose ApplicationAssembly để đăng ký handlers + validators.
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssemblies(
                IdentityModuleExtensions.ApplicationAssembly
                // CourseModuleExtensions.ApplicationAssembly,    
                // EnrollmentModuleExtensions.ApplicationAssembly,
                // ReportingModuleExtensions.ApplicationAssembly
            );

            // Pipeline behavior: validate trước khi handler chạy.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Scan validators từ cùng assemblies.
        services.AddValidatorsFromAssembly(
            IdentityModuleExtensions.ApplicationAssembly,
               includeInternalTypes: true);
    }
}