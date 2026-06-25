using CourseManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;

namespace CourseManagement.Domain.Events;

// Published when an existing class session's date or type is changed.
public sealed record ClassSessionUpdatedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid CourseId { get; init; }
    public Guid ClassSessionId { get; init; }
    public DateOnly NewSessionDate { get; init; }
    public SessionType NewSessionType { get; init; }

    public static ClassSessionUpdatedEvent Create(
        Guid courseId,
        Guid classSessionId,
        DateOnly newSessionDate,
        SessionType newSessionType) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            CourseId = courseId,
            ClassSessionId = classSessionId,
            NewSessionDate = newSessionDate,
            NewSessionType = newSessionType
        };
}