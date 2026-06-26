using System.Reflection;
using Enrollment.Domain.Repositories;
using Enrollment.Infrastructure.Persistence;
using Enrollment.Infrastructure.Repositories;
using Enrollment.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions;

namespace Enrollment.Infrastructure.Extensions;

public static class EnrollmentModuleExtensions {
    public static readonly Assembly ApplicationAssembly =
        typeof(Enrollment.Application.Enrollments.EnrollCourse.EnrollCourseCommand).Assembly;

    public static IServiceCollection AddEnrollmentModule(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddDbContext<EnrollmentDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("EnrollmentDb")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EnrollmentDbContext>());
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();

        services.AddScoped<EnrollmentQueryService>();
        services.AddScoped<CourseManagement.Application.Common.Interfaces.IEnrollmentCourseService>(
            sp => sp.GetRequiredService<EnrollmentQueryService>());
        services.AddScoped<Identity.Application.Common.Interfaces.IEnrollmentIdentityService>(
            sp => sp.GetRequiredService<EnrollmentQueryService>());

        services.AddScoped<Enrollment.Application.Common.Interfaces.INotificationService, SignalRNotificationService>();

        services.AddSignalR();

        return services;
    }
}