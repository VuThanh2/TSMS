using SharedKernel.Abstractions;

namespace Identity.Domain.Events;

// Published when a new user account is created.
// Consumed by Reporting BC to sync user data into projections.
public sealed record UserCreatedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
 
    public static UserCreatedEvent Create(Guid userId, string fullName, string email, string role) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            UserId = userId,
            FullName = fullName,
            Email = email,
            Role = role
        };
}