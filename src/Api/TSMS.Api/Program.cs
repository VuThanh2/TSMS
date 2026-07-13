using CourseManagement.Infrastructure.Extensions;
using EnrollmentManagement.Infrastructure.Extensions;
using EnrollmentManagement.Infrastructure.Hubs;
using Hangfire;
using Hangfire.Dashboard;
using Identity.Infrastructure.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Reporting.Infrastructure.Extensions;
using TSMS.Api.Extensions;
using TSMS.Api.Hangfire;
using TSMS.Api.HealthChecks;
using TSMS.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Modules
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddCourseModule(builder.Configuration);
builder.Services.AddEnrollmentModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);

// ── Cross-cutting
builder.Services.AddApiServices(builder.Configuration);

// ── OpenAPI / Swagger
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// ── Seed
await app.SeedIdentityDataAsync();

// ── Middleware pipeline
if (app.Environment.IsDevelopment()) {
    app.UseSwaggerDocumentation();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Map SAU Authentication/Authorization để HttpContext.User đã sẵn sàng khi filter chạy.
// Không còn gate theo IsDevelopment() — Dashboard cần dùng được cả trên Railway lúc demo
app.UseHangfireDashboard(
    app.Configuration["Hangfire:Dashboard"] ?? "/hangfire",
    new DashboardOptions {
        Authorization = [new AdminOnlyDashboardAuthorizationFilter()]
    });

app.MapControllers();
app.MapGet("/", () => Results.Ok(new { service = "TSMS API", status = "Running" }));
app.MapHealthChecks("/health", new HealthCheckOptions {
    ResponseWriter = HealthCheckResponseWriter.WriteJsonResponse
});
app.MapHub<GradeHub>("/hubs/grade");

// ── Hangfire jobs
app.RegisterCourseJobs();
app.RegisterEnrollmentJobs();

app.Run();