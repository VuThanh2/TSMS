using CourseManagement.Domain.Entities;
using CourseManagement.Domain.Events;
using CourseManagement.Domain.ValueObjects;
using CourseManagement.Infrastructure.Persistence;
using CourseManagement.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Reporting.Domain.Repositories;
using Reporting.Infrastructure.EventHandlers;
using Reporting.Infrastructure.Persistence;
using Reporting.Infrastructure.Repositories;

namespace Reporting.UnitTests;

// Tái hiện đúng kịch bản lỗi user báo cáo: "tạo 1 Course và Get thử thì Course đó vẫn tồn tại
// bình thường nhưng khi check Reporting thì lại có lỗi 404". Đi qua đúng pipeline thật:
// Course.Create() → CourseDbContext.SaveChangesAsync (BaseDbContext ghi OutboxMessage cùng
// transaction) → CourseOutboxProcessor.ExecuteAsync (giả lập đúng 1 lần Hangfire job chạy,
// thay vì phải chờ 1 phút thật) → MediatR publish → CourseEventHandler ghi vào
// ReportingDbContext. Nếu 1 trong 2 bug cũ (job không được schedule, handler đăng ký sai DI)
// còn tồn tại, test này sẽ fail.
public class OutboxToReportingProjectionTests {
    [Fact]
    public async Task CreatingCourse_ThenRunningOutboxJobOnce_PopulatesReportingProjection() {
        // Arrange — CourseDbContext đứng vai CourseManagement BC thật.
        var courseDbOptions = new DbContextOptionsBuilder<CourseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var courseDb = new CourseDbContext(courseDbOptions);

        // ReportingDbContext đứng vai Reporting BC thật — dùng 1 instance xuyên suốt test
        // để có thể assert dữ liệu sau khi job publish xong.
        var reportingDbOptions = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var reportingDb = new ReportingDbContext(reportingDbOptions);

        // DI wiring — cùng shape với ReportingModuleExtensions.AddReportingModule:
        // 1 instance CourseEventHandler, forward qua interface INotificationHandler<TEvent>.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IReportingRepository>(_ => new ReportingRepository(reportingDb));
        services.AddScoped<CourseEventHandler>();
        services.AddScoped<INotificationHandler<CourseCreatedEvent>>(
            sp => sp.GetRequiredService<CourseEventHandler>());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CourseEventHandler).Assembly));

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // Act 1 — Admin tạo Course mới (raise CourseCreatedEvent, ghi Outbox trong cùng transaction).
        var course = Course.Create(
            lecturerId: Guid.NewGuid(),
            courseName: CourseName.Create("OOP Advanced").Value,
            description: null,
            dateRange: DateRange.Create(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3))).Value,
            maxCapacity: 30,
            lecturerName: "Nguyen Van A").Value;

        courseDb.Courses.Add(course);
        await courseDb.SaveChangesAsync();

        // Course đã tồn tại ngay lúc này (đúng như user quan sát — GET Course vẫn ra 200).
        Assert.True(await courseDb.Courses.AnyAsync(c => c.Id == course.Id));

        // Act 2 — mô phỏng đúng 1 lần Hangfire recurring job "process-course-outbox" chạy.
        var outboxProcessor = new CourseOutboxProcessor(
            courseDb, publisher, NullLogger<CourseOutboxProcessor>.Instance);
        await outboxProcessor.ExecuteAsync();

        // Assert — Reporting phải có projection cho Course vừa tạo (trước fix: luôn rỗng → 404).
        var stats = await reportingDb.CourseStatistics.SingleOrDefaultAsync(v => v.CourseId == course.Id);
        Assert.NotNull(stats);
        Assert.Equal("OOP Advanced", stats!.CourseName);
        Assert.Equal("Upcoming", stats.Status);

        var outboxMessage = await courseDb.OutboxMessages.SingleAsync();
        Assert.NotNull(outboxMessage.ProcessedOn);
    }
}
