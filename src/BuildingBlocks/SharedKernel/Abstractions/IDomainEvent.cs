using MediatR;

namespace SharedKernel.Abstractions;

// Extend INotification để MediatR có thể dispatch domain events
// đến INotificationHandler<T> đã đăng ký trong cùng process.
public interface IDomainEvent : INotification {
    Guid EventId { get; init; }
    DateTime OccurredOn { get; init; }
}