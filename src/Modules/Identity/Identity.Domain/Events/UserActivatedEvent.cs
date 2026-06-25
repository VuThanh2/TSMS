using SharedKernel.Abstractions;

namespace Identity.Domain.Events;

// Published when an inactive account is reactivated by Admin.
// Application Layer must also call UserManager.SetLockoutEndDateAsync(user, null).
public sealed record UserActivatedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid UserId { get; init; }
 
    public static UserActivatedEvent Create(Guid userId) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            UserId = userId
        };
}