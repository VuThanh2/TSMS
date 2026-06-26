using System.Reflection;
using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Domain.Repositories;
using CourseManagement.Infrastructure.Persistence;
using CourseManagement.Infrastructure.Repositories;
using CourseManagement.Infrastructure.Services;
using Hangfire;
using Identity.Application.Common.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions;

namespace CourseManagement.Infrastructure.Extensions;

public static class CourseModuleExtensions {
    // Expose Application assembly so Program.cs can scan MediatR handlers and FluentValidation validators.
    public static readonly Assembly ApplicationAssembly =
        typeof(CourseManagement.Application.Courses.CreateCourse.CreateCourseCommand).Assembly;

    public static IServiceCollection AddCourseModule(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddDbContext<CourseDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("CourseDb")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CourseDbContext>());
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<CourseQueryService>();
        services.AddScoped<ICourseQueryService>(sp => sp.GetRequiredService<CourseQueryService>());
        services.AddScoped<ICourseLookupService>(sp => sp.GetRequiredService<CourseQueryService>());
        services.AddScoped<UpdateCourseStatusJobService>();

        return services;
    }

    /// Registers the recurring Hangfire job for automatic course status transitions.
    public static void RegisterCourseJobs(this WebApplication app) {
        RecurringJob.AddOrUpdate<UpdateCourseStatusJobService>(
            recurringJobId: "update-course-status",
            methodCall: job => job.ExecuteAsync(CancellationToken.None),
            cronExpression: "5 0 * * *");
    }
}