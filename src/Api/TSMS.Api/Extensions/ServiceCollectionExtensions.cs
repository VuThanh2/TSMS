using CourseManagement.Infrastructure.Extensions;
using CourseManagement.Infrastructure.Persistence;
using CourseManagement.Presentation.Controllers;
using EnrollmentManagement.Infrastructure.Extensions;
using EnrollmentManagement.Infrastructure.Persistence;
using EnrollmentManagement.Presentation.Controllers;
using FluentValidation;
using Hangfire;
using Identity.Infrastructure.Extensions;
using Identity.Infrastructure.Persistence;
using Identity.Presentation.Controllers;
using Reporting.Infrastructure.Extensions;
using Reporting.Infrastructure.Persistence;
using Reporting.Presentation.Controllers;
using SharedInfrastructure.Email;
using TSMS.Api.Options;

namespace TSMS.Api.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration) {
        services
            .AddControllers()
            // Scan Presentation assemblies để đăng ký controllers từ từng module.
            .AddApplicationPart(typeof(AuthenticationController).Assembly)
            .AddApplicationPart(typeof(CourseController).Assembly)
            .AddApplicationPart(typeof(EnrollmentsController).Assembly)
            .AddApplicationPart(typeof(ReportingController).Assembly);
        services.AddCorsPolicy(configuration);
        services.AddMediatRWithValidation();
        services.AddHangfireServices(configuration);
        services.AddHealthCheckServices();
        services.AddEmailServices(configuration);

        return services;
    }

    // ── Private helpers

    private static void AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
    }
    
    private static void AddHealthCheckServices(this IServiceCollection services) {
        services.AddHealthChecks()
            .AddDbContextCheck<IdentityDbContext>(name: "IdentityDb")
            .AddDbContextCheck<CourseDbContext>(name: "CourseDb")
            .AddDbContextCheck<EnrollmentDbContext>(name: "EnrollmentDb")
            .AddDbContextCheck<ReportingDbContext>(name: "ReportingDb");
    }
    
    private static void AddHangfireServices(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("CourseDb")));
 
        services.AddHangfireServer();
    }
    
    private static void AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration) {
        var corsOptions = configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>() ?? new CorsOptions();

        services.AddCors(options => {
            options.AddPolicy("AllowFrontend", policy => {
                policy.WithOrigins(corsOptions.AllowedOrigins)
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
                IdentityModuleExtensions.ApplicationAssembly,
                CourseModuleExtensions.ApplicationAssembly, 
                EnrollmentModuleExtensions.ApplicationAssembly,
                ReportingModuleExtensions.ApplicationAssembly
            );

            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Scan validators từ cùng assemblies.
        services.AddValidatorsFromAssemblies([
            IdentityModuleExtensions.ApplicationAssembly,
            CourseModuleExtensions.ApplicationAssembly,
            EnrollmentModuleExtensions.ApplicationAssembly,
            ReportingModuleExtensions.ApplicationAssembly
        ], includeInternalTypes: true);
    }
}