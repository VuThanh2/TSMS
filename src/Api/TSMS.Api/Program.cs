using CourseManagement.Infrastructure.Extensions;
using EnrollmentManagement.Infrastructure.Extensions;
using EnrollmentManagement.Infrastructure.Services;
using Identity.Infrastructure.Extensions;
using Reporting.Infrastructure.Extensions;
using TSMS.Api.Extensions;
using TSMS.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Modules
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddCourseModule(builder.Configuration);
builder.Services.AddEnrollmentModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);

// ── Cross-cutting
builder.Services.AddApiServices(builder.Configuration);

// ── OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Seed
await app.SeedIdentityDataAsync();

// ── Middleware pipeline
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GradeHub>("/hubs/grade");

// ── Hangfire jobs
app.RegisterCourseJobs();

app.Run();