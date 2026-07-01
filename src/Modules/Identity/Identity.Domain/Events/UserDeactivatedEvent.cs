using SharedKernel.Abstractions;

namespace Identity.Domain.Events;

// Published when an active account is deactivated by Admin.
// Application Layer must also call UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue)
// so MapIdentityApi's /login endpoint respects the deactivation (it only checks LockoutEnd).
public sealed record UserDeactivatedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid UserId { get; init; }
 
    public static UserDeactivatedEvent Create(Guid userId) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            UserId = userId
        };
}