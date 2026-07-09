using System.Text.Json;
using Enrollment.UnitTests.Fakes;
using EnrollmentManagement.Domain.Events;
using EnrollmentManagement.Infrastructure.Persistence;
using EnrollmentManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SharedInfrastructure.Outbox;

namespace Enrollment.UnitTests;

// EnrollmentOutboxProcessor cũng là Hangfire recurring job (bị bỏ sót lịch chạy — cùng lỗi
// với CourseOutboxProcessor). Test giả lập 1 lần chạy job thay vì chờ Hangfire trigger.
public class EnrollmentOutboxProcessorTests {
    private static EnrollmentDbContext CreateInMemoryContext() {
        var options = new DbContextOptionsBuilder<EnrollmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EnrollmentDbContext(options);
    }

    private static OutboxMessage CreateStudentEnrolledMessage(DateTime occurredOn) {
        var domainEvent = StudentEnrolledEvent.Create(
            enrollmentId: Guid.NewGuid(),
            studentId: Guid.NewGuid(),
            courseId: Guid.NewGuid(),
            enrolledAt: DateTime.UtcNow,
            studentFullName: "Tran Thi B",
            studentEmail: "b.tran@example.com",
            courseName: "OOP Advanced",
            courseStatus: "Upcoming",
            totalSessionsInCourse: 20);

        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

        return OutboxMessage.Create(
            id: domainEvent.EventId,
            type: typeof(StudentEnrolledEvent).AssemblyQualifiedName!,
            payload: payload,
            occurredOn: occurredOn);
    }

    [Fact]
    public async Task ExecuteAsync_PublishesPendingMessage_AndMarksProcessed() {
        await using var context = CreateInMemoryContext();
        var message = CreateStudentEnrolledMessage(DateTime.UtcNow);
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var publisher = new FakePublisher();
        var processor = new EnrollmentOutboxProcessor(context, publisher, NullLogger<EnrollmentOutboxProcessor>.Instance);

        await processor.ExecuteAsync();

        Assert.Single(publisher.PublishedNotifications);
        Assert.IsType<StudentEnrolledEvent>(publisher.PublishedNotifications[0]);

        var reloaded = await context.OutboxMessages.SingleAsync(m => m.Id == message.Id);
        Assert.NotNull(reloaded.ProcessedOn);
        Assert.Null(reloaded.Error);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsMessagesAlreadyProcessed() {
        await using var context = CreateInMemoryContext();
        var message = CreateStudentEnrolledMessage(DateTime.UtcNow);
        message.MarkProcessed(DateTime.UtcNow);
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var publisher = new FakePublisher();
        var processor = new EnrollmentOutboxProcessor(context, publisher, NullLogger<EnrollmentOutboxProcessor>.Instance);

        await processor.ExecuteAsync();

        Assert.Empty(publisher.PublishedNotifications);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPublishThrows_MarksFailed_AndLeavesMessageForRetry() {
        await using var context = CreateInMemoryContext();
        var message = CreateStudentEnrolledMessage(DateTime.UtcNow);
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var publisher = new FakePublisher {
            ExceptionToThrow = new InvalidOperationException("Reporting DB unavailable")
        };
        var processor = new EnrollmentOutboxProcessor(context, publisher, NullLogger<EnrollmentOutboxProcessor>.Instance);

        await processor.ExecuteAsync();

        var reloaded = await context.OutboxMessages.SingleAsync(m => m.Id == message.Id);
        Assert.Null(reloaded.ProcessedOn);
        Assert.Equal("Reporting DB unavailable", reloaded.Error);
    }
}
