using SharedKernel.Abstractions;

namespace Identity.Domain.Events;

// Published when a user's full name, email, or profile fields are updated.
public sealed record UserUpdatedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
 
    public static UserUpdatedEvent Create(Guid userId, string fullName, string email) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            UserId = userId,
            FullName = fullName,
            Email = email
        };
}