using System.Reflection;
using CourseManagement.Domain.Events;
using EnrollmentManagement.Domain.Events;
using Identity.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reporting.Application.Common.Interfaces;
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

        // Demo Data Reset (dev-only) — bulk delete cả 5 ReadModel.
        services.AddScoped<IReportingDataResetter, ReportingDataResetter>();

        // EventHandlers sống ở Infrastructure (không phải Application) nên MediatR's
        // RegisterServicesFromAssemblies (chỉ scan *.Application assembly của từng module)
        // không tự phát hiện được — phải đăng ký thủ công từng INotificationHandler<TEvent>.
        // 1 instance implement nhiều interface → forward DI (cùng convention với cross-BC interface).
        services.AddScoped<CourseEventHandler>();
        services.AddScoped<INotificationHandler<CourseCreatedEvent>>(sp => sp.GetRequiredService<CourseEventHandler>());
        services.AddScoped<INotificationHandler<CourseUpdatedEvent>>(sp => sp.GetRequiredService<CourseEventHandler>());
        services.AddScoped<INotificationHandler<CourseStatusChangedEvent>>(sp => sp.GetRequiredService<CourseEventHandler>());
        services.AddScoped<INotificationHandler<LecturerReplacedEvent>>(sp => sp.GetRequiredService<CourseEventHandler>());
        services.AddScoped<INotificationHandler<CourseDeletedEvent>>(sp => sp.GetRequiredService<CourseEventHandler>());

        services.AddScoped<EnrollmentEventHandler>();
        services.AddScoped<INotificationHandler<StudentEnrolledEvent>>(sp => sp.GetRequiredService<EnrollmentEventHandler>());
        services.AddScoped<INotificationHandler<GradeAssignedEvent>>(sp => sp.GetRequiredService<EnrollmentEventHandler>());
        services.AddScoped<INotificationHandler<GradeUpdatedEvent>>(sp => sp.GetRequiredService<EnrollmentEventHandler>());

        services.AddScoped<AttendanceEventHandler>();
        services.AddScoped<INotificationHandler<AttendanceMarkedEvent>>(sp => sp.GetRequiredService<AttendanceEventHandler>());

        // Identity events: đến trực tiếp qua IPublisher từ Application handlers (không qua Outbox).
        services.AddScoped<IdentityEventHandler>();
        services.AddScoped<INotificationHandler<UserUpdatedEvent>>(sp => sp.GetRequiredService<IdentityEventHandler>());

        return services;
    }
}