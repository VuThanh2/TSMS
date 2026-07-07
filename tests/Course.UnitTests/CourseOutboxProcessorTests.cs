using System.Text.Json;
using Course.UnitTests.Fakes;
using CourseManagement.Domain.Events;
using CourseManagement.Infrastructure.Persistence;
using CourseManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SharedInfrastructure.Outbox;

namespace Course.UnitTests;

// CourseOutboxProcessor là Hangfire recurring job — chạy nền, không có endpoint để gọi trực tiếp
// từ Swagger/Postman. Test này giả lập đúng 1 lần chạy job (ExecuteAsync) thay vì chờ Hangfire
// tự trigger theo cron, nên không cần dựng lịch/đợi thời gian thật để kiểm tra logic.
public class CourseOutboxProcessorTests {
    private static CourseDbContext CreateInMemoryContext() {
        var options = new DbContextOptionsBuilder<CourseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CourseDbContext(options);
    }

    private static OutboxMessage CreateCourseCreatedMessage(DateTime occurredOn, Guid? courseId = null) {
        var domainEvent = CourseCreatedEvent.Create(
            courseId: courseId ?? Guid.NewGuid(),
            lecturerId: Guid.NewGuid(),
            courseName: "OOP Advanced",
            lecturerName: "Nguyen Van A",
            startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),
            maxCapacity: 30);

        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

        return OutboxMessage.Create(
            id: domainEvent.EventId,
            type: typeof(CourseCreatedEvent).AssemblyQualifiedName!,
            payload: payload,
            occurredOn: occurredOn);
    }

    [Fact]
    public async Task ExecuteAsync_PublishesPendingMessage_AndMarksProcessed() {
        // Arrange
        await using var context = CreateInMemoryContext();
        var message = CreateCourseCreatedMessage(DateTime.UtcNow);
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var publisher = new FakePublisher();
        var processor = new CourseOutboxProcessor(context, publisher, NullLogger<CourseOutboxProcessor>.Instance);

        // Act
        await processor.ExecuteAsync();

        // Assert — đây chính là bug đã fix: nếu job không chạy, event không bao giờ tới Reporting
        // và CourseStatisticsView sẽ không tồn tại, dẫn tới 404 khi GET Reporting.
        Assert.Single(publisher.PublishedNotifications);
        Assert.IsType<CourseCreatedEvent>(publisher.PublishedNotifications[0]);

        var reloaded = await context.OutboxMessages.SingleAsync(m => m.Id == message.Id);
        Assert.NotNull(reloaded.ProcessedOn);
        Assert.Null(reloaded.Error);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsMessagesAlreadyProcessed() {
        await using var context = CreateInMemoryContext();
        var message = CreateCourseCreatedMessage(DateTime.UtcNow);
        message.MarkProcessed(DateTime.UtcNow);
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var publisher = new FakePublisher();
        var processor = new CourseOutboxProcessor(context, publisher, NullLogger<CourseOutboxProcessor>.Instance);

        await processor.ExecuteAsync();

        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownEventType_MarksProcessedWithoutPublishing() {
        await using var context = CreateInMemoryContext();
        var message = OutboxMessage.Create(
            id: Guid.NewGuid(),
            type: "Namespace.Does.Not.Exist, MissingAssembly",
            payload: "{}",
            occurredOn: DateTime.UtcNow);
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var publisher = new FakePublisher();
        var processor = new CourseOutboxProcessor(context, publisher, NullLogger<CourseOutboxProcessor>.Instance);

        await processor.ExecuteAsync();

        Assert.Empty(publisher.PublishedNotifications);
        var reloaded = await context.OutboxMessages.SingleAsync(m => m.Id == message.Id);
        Assert.NotNull(reloaded.ProcessedOn);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPublishThrows_MarksFailed_AndLeavesMessageForRetry() {
        await using var context = CreateInMemoryContext();
        var message = CreateCourseCreatedMessage(DateTime.UtcNow);
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var publisher = new FakePublisher {
            ExceptionToThrow = new InvalidOperationException("Reporting DB unavailable")
        };
        var processor = new CourseOutboxProcessor(context, publisher, NullLogger<CourseOutboxProcessor>.Instance);

        await processor.ExecuteAsync();

        // Message KHÔNG được đánh dấu processed — lần chạy job kế tiếp (1 phút sau) sẽ retry.
        var reloaded = await context.OutboxMessages.SingleAsync(m => m.Id == message.Id);
        Assert.Null(reloaded.ProcessedOn);
        Assert.Equal("Reporting DB unavailable", reloaded.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesMultiplePendingMessages_InOccurredOnOrder() {
        await using var context = CreateInMemoryContext();
        var older = CreateCourseCreatedMessage(DateTime.UtcNow.AddMinutes(-5));
        var newer = CreateCourseCreatedMessage(DateTime.UtcNow);
        context.OutboxMessages.AddRange(newer, older);
        await context.SaveChangesAsync();

        var publisher = new FakePublisher();
        var processor = new CourseOutboxProcessor(context, publisher, NullLogger<CourseOutboxProcessor>.Instance);

        await processor.ExecuteAsync();

        Assert.Equal(2, publisher.PublishedNotifications.Count);
        var firstPublished = Assert.IsType<CourseCreatedEvent>(publisher.PublishedNotifications[0]);
        Assert.Equal(older.Id, firstPublished.EventId);
    }

    [Fact]
    public async Task ExecuteAsync_MalformedJsonPayload_MarksFailed_ButStillProcessesOtherPendingMessages() {
        // Một message bị hỏng payload (VD: dữ liệu cũ trước khi đổi shape event) không được
        // phép chặn đứng cả batch — các message hợp lệ khác vẫn phải được publish bình thường.
        await using var context = CreateInMemoryContext();
        var corrupted = OutboxMessage.Create(
            id: Guid.NewGuid(),
            type: typeof(CourseCreatedEvent).AssemblyQualifiedName!,
            payload: "{ this is not valid json",
            occurredOn: DateTime.UtcNow.AddMinutes(-1));
        var healthy = CreateCourseCreatedMessage(DateTime.UtcNow);
        context.OutboxMessages.AddRange(corrupted, healthy);
        await context.SaveChangesAsync();

        var publisher = new FakePublisher();
        var processor = new CourseOutboxProcessor(context, publisher, NullLogger<CourseOutboxProcessor>.Instance);

        await processor.ExecuteAsync();

        Assert.Single(publisher.PublishedNotifications);

        var reloadedCorrupted = await context.OutboxMessages.SingleAsync(m => m.Id == corrupted.Id);
        Assert.Null(reloadedCorrupted.ProcessedOn);
        Assert.NotNull(reloadedCorrupted.Error);

        var reloadedHealthy = await context.OutboxMessages.SingleAsync(m => m.Id == healthy.Id);
        Assert.NotNull(reloadedHealthy.ProcessedOn);
    }

    [Fact]
    public async Task ExecuteAsync_PayloadDeserializesToNull_MarksProcessedWithoutPublishing() {
        await using var context = CreateInMemoryContext();
        var message = OutboxMessage.Create(
            id: Guid.NewGuid(),
            type: typeof(CourseCreatedEvent).AssemblyQualifiedName!,
            payload: "null",
            occurredOn: DateTime.UtcNow);
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var publisher = new FakePublisher();
        var processor = new CourseOutboxProcessor(context, publisher, NullLogger<CourseOutboxProcessor>.Instance);

        await processor.ExecuteAsync();

        Assert.Empty(publisher.PublishedNotifications);
        var reloaded = await context.OutboxMessages.SingleAsync(m => m.Id == message.Id);
        Assert.NotNull(reloaded.ProcessedOn);
    }
}
