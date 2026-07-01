using SharedKernel.Abstractions;

namespace Identity.Domain.Events;

// Published when a user explicitly logs out via POST /api/auth/logout.
public sealed record UserLoggedOutEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid UserId { get; init; }
 
    public static UserLoggedOutEvent Create(Guid userId) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            UserId = userId
        };
}