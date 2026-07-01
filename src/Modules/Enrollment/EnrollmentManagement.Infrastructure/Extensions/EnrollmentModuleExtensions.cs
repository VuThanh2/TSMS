using System.Reflection;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Domain.Repositories;
using EnrollmentManagement.Infrastructure.Persistence;
using EnrollmentManagement.Infrastructure.Repositories;
using EnrollmentManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedInfrastructure.Persistence;
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

        services.AddScoped<EnrollmentQueryService>();
        services.AddScoped<CourseManagement.Application.Common.Interfaces.IEnrollmentCourseService>(
            sp => sp.GetRequiredService<EnrollmentQueryService>());
        services.AddScoped<Identity.Application.Common.Interfaces.IEnrollmentIdentityService>(
            sp => sp.GetRequiredService<EnrollmentQueryService>());

        services.AddScoped<EnrollmentManagement.Application.Common.Interfaces.INotificationService, SignalRNotificationService>();

        services.AddSignalR();

        return services;
    }
}