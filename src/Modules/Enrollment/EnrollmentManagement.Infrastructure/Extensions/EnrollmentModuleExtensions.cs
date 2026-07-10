using System.Reflection;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Application.Common.Services;
using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentManagement.Infrastructure.Persistence;
using EnrollmentManagement.Infrastructure.Repositories;
using EnrollmentManagement.Infrastructure.Services;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedInfrastructure.Persistence;
using SharedInfrastructure.Time;
using SharedKernel.Abstractions;

namespace EnrollmentManagement.Infrastructure.Extensions;

public static class EnrollmentModuleExtensions {
    public static readonly Assembly ApplicationAssembly =
        typeof(EnrollmentManagement.Application.Enrollments.EnrollCourse.EnrollCourseCommand).Assembly;

    public static IServiceCollection AddEnrollmentModule(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddDbContext<EnrollmentDbContext>(options =>
            options.UseTsmsSqlServer(configuration.GetConnectionString("EnrollmentDb")!, TsmsSchemas.Enrollment));

        services.AddScoped<IEnrollmentUnitOfWork>(sp => sp.GetRequiredService<EnrollmentDbContext>());
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IScheduleConflictChecker, ScheduleConflictChecker>();

        services.AddScoped<EnrollmentQueryService>();
        services.AddScoped<CourseManagement.Application.Common.Interfaces.IEnrollmentCourseService>(
            sp => sp.GetRequiredService<EnrollmentQueryService>());

        // Cross-BC: Course nhờ Enrollment back-fill Attendance khi lịch học đổi (gia hạn EndDate).
        services.AddScoped<CourseManagement.Application.Common.Interfaces.IEnrollmentAttendanceSync,
            EnrollmentAttendanceSyncService>();
        services.AddScoped<Identity.Application.Common.Interfaces.IEnrollmentIdentityService>(
            sp => sp.GetRequiredService<EnrollmentQueryService>());

        services.AddScoped<INotificationService, SignalRNotificationService>();
        services.AddScoped<EnrollmentOutboxProcessor>();
        services.AddScoped<SendSessionReminderJobService>();

        services.AddSignalR();

        return services;
    }

    /// Registers the recurring Hangfire job dispatch OutboxMessages của EnrollmentManagement BC.
    public static void RegisterEnrollmentJobs(this WebApplication app) {
        using var scope = app.Services.CreateScope();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        recurringJobManager.AddOrUpdate<EnrollmentOutboxProcessor>(
            recurringJobId: "process-enrollment-outbox",
            methodCall: job => job.ExecuteAsync(CancellationToken.None),
            cronExpression: "*/1 * * * *");

        // 30 phút trước mỗi ca học cố định (Sáng 07:00, Chiều 13:00) theo GIỜ VN. Cron mặc định
        // của Hangfire chạy theo UTC, nên bắt buộc truyền TimeZone = VN để 06:30/12:30 là giờ địa
        // phương — khớp với cách SendSessionReminderJobService tính "hôm nay" theo giờ VN.
        var vnJobOptions = new RecurringJobOptions { TimeZone = VietnamTimeZone.Instance };

        recurringJobManager.AddOrUpdate<SendSessionReminderJobService>(
            recurringJobId: "send-morning-session-reminder",
            methodCall: job => job.ExecuteAsync(SessionType.Morning, CancellationToken.None),
            cronExpression: "30 6 * * *",
            options: vnJobOptions);

        recurringJobManager.AddOrUpdate<SendSessionReminderJobService>(
            recurringJobId: "send-afternoon-session-reminder",
            methodCall: job => job.ExecuteAsync(SessionType.Afternoon, CancellationToken.None),
            cronExpression: "30 12 * * *",
            options: vnJobOptions);
    }
}