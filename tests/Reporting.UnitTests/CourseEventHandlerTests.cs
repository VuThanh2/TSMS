using CourseManagement.Domain.Events;
using CourseManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Reporting.Domain.ReadModels;
using Reporting.Infrastructure.EventHandlers;
using Reporting.Infrastructure.Persistence;
using Reporting.Infrastructure.Repositories;

namespace Reporting.UnitTests;

// Test trực tiếp logic của EventHandler (không qua Outbox/MediatR) — đảm bảo khi event
// tới đúng handler, projection được ghi đúng dữ liệu. Việc handler có "tới được" hay không
// (vấn đề DI wiring) được test riêng ở ReportingModuleDiWiringTests.
public class CourseEventHandlerTests {
    private static (ReportingDbContext Context, CourseEventHandler Handler) CreateSut() {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ReportingDbContext(options);
        var repository = new ReportingRepository(context);
        var handler = new CourseEventHandler(repository, NullLogger<CourseEventHandler>.Instance);
        return (context, handler);
    }

    [Fact]
    public async Task Handle_CourseCreatedEvent_CreatesCourseStatisticsAndFourScoreDistributionRows() {
        // Arrange
        var (context, handler) = CreateSut();
        var courseId = Guid.NewGuid();
        var lecturerId = Guid.NewGuid();
        var domainEvent = CourseCreatedEvent.Create(
            courseId: courseId,
            lecturerId: lecturerId,
            courseName: "OOP Advanced",
            lecturerName: "Nguyen Van A",
            startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),
            maxCapacity: 30);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert — đây là hành vi mà production bug đã chặn đứng hoàn toàn:
        // CourseStatisticsView phải tồn tại ngay sau khi Course được tạo.
        var stats = await context.CourseStatistics.SingleAsync(v => v.CourseId == courseId);
        Assert.Equal("OOP Advanced", stats.CourseName);
        Assert.Equal(lecturerId, stats.LecturerId);
        Assert.Equal("Upcoming", stats.Status);
        Assert.Equal(0, stats.EnrolledCount);
        Assert.Null(stats.AverageScore);

        var distributions = await context.ScoreDistributions
            .Where(v => v.CourseId == courseId)
            .ToListAsync();
        Assert.Equal(4, distributions.Count);
        Assert.All(distributions, d => Assert.Equal(0, d.StudentCount));
        Assert.Contains(distributions, d => d.ScoreGroup == ScoreGroups.Excellent);
        Assert.Contains(distributions, d => d.ScoreGroup == ScoreGroups.Good);
        Assert.Contains(distributions, d => d.ScoreGroup == ScoreGroups.Average);
        Assert.Contains(distributions, d => d.ScoreGroup == ScoreGroups.Weak);
    }

    [Fact]
    public async Task Handle_CourseCreatedEvent_WhenStatisticsAlreadyExist_SkipsWithoutDuplicating() {
        // Arrange — mô phỏng trường hợp OutboxProcessor publish trùng message (at-least-once delivery).
        var (context, handler) = CreateSut();
        var courseId = Guid.NewGuid();
        context.CourseStatistics.Add(CourseStatisticsView.Create(
            courseId: courseId,
            lecturerId: Guid.NewGuid(),
            courseName: "Existing Course",
            lecturerName: "Le Van C",
            startDate: DateOnly.FromDateTime(DateTime.UtcNow),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            status: "Upcoming"));
        await context.SaveChangesAsync();

        var domainEvent = CourseCreatedEvent.Create(
            courseId: courseId,
            lecturerId: Guid.NewGuid(),
            courseName: "OOP Advanced",
            lecturerName: "Nguyen Van A",
            startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),
            maxCapacity: 30);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert — vẫn chỉ có đúng 1 row, giữ nguyên dữ liệu cũ, không ghi đè.
        var allStats = await context.CourseStatistics.Where(v => v.CourseId == courseId).ToListAsync();
        Assert.Single(allStats);
        Assert.Equal("Existing Course", allStats[0].CourseName);
        Assert.Empty(await context.ScoreDistributions.Where(v => v.CourseId == courseId).ToListAsync());
    }

    [Fact]
    public async Task Handle_CourseStatusChangedEvent_UpdatesStatusOnExistingStatistics() {
        var (context, handler) = CreateSut();
        var courseId = Guid.NewGuid();
        context.CourseStatistics.Add(CourseStatisticsView.Create(
            courseId: courseId,
            lecturerId: Guid.NewGuid(),
            courseName: "OOP Advanced",
            lecturerName: "Nguyen Van A",
            startDate: DateOnly.FromDateTime(DateTime.UtcNow),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),
            status: "Upcoming"));
        await context.SaveChangesAsync();

        var statusChanged = CourseStatusChangedEvent.Create(courseId, CourseStatus.Active);

        await handler.Handle(statusChanged, CancellationToken.None);

        var stats = await context.CourseStatistics.SingleAsync(v => v.CourseId == courseId);
        Assert.Equal("Active", stats.Status);
    }
}
