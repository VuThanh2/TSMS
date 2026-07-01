using SharedKernel.Abstractions;

namespace CourseManagement.Domain.Events;

// Published when a class session is removed from a course.
public sealed record ClassSessionRemovedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid CourseId { get; init; }
    public Guid ClassSessionId { get; init; }

    public static ClassSessionRemovedEvent Create(Guid courseId, Guid classSessionId) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            CourseId = courseId,
            ClassSessionId = classSessionId
        };
}