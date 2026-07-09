using MediatR;

namespace Enrollment.UnitTests.Fakes;

// Fake thay cho MediatR IPublisher thật — tránh phải quay lại DI container đầy đủ
// (bao gồm cả Reporting module) chỉ để test riêng logic của EnrollmentOutboxProcessor.
public sealed class FakePublisher : IPublisher {
    public List<object> PublishedNotifications { get; } = [];
    public Exception? ExceptionToThrow { get; set; }

    public Task Publish(object notification, CancellationToken cancellationToken = default) {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        PublishedNotifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        PublishedNotifications.Add(notification!);
        return Task.CompletedTask;
    }
}
