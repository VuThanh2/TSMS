using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reporting.Domain.Repositories;
using Reporting.Infrastructure.EventHandlers;
using Reporting.Infrastructure.Persistence;
using Reporting.Infrastructure.Repositories;
using SharedInfrastructure.Persistence;

namespace Reporting.Infrastructure.Extensions;

public static class ReportingModuleExtensions {
    public static readonly Assembly ApplicationAssembly =
        typeof(Reporting.Application.CourseStatistics.GetCourseStatistics.GetCourseStatisticsQuery).Assembly;

    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddDbContext<ReportingDbContext>(options =>
            options.UseTsmsSqlServer(configuration.GetConnectionString("ReportingDb")!, TsmsSchemas.Reporting));

        services.AddScoped<IReportingRepository, ReportingRepository>();

        // EventHandlers — MediatR tự dispatch khi các BC publish event.
        // Course/Enrollment events: đến qua OutboxProcessor → IPublisher → handler.
        // Identity events: đến trực tiếp qua IPublisher từ Application handlers.
        services.AddScoped<CourseEventHandler>();
        services.AddScoped<EnrollmentEventHandler>();
        services.AddScoped<AttendanceEventHandler>();
        services.AddScoped<IdentityEventHandler>();

        return services;
    }
}