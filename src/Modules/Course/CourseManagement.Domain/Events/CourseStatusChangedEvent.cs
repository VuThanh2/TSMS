using CourseManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;

namespace CourseManagement.Domain.Events;

// Published when the background job transitions a course's status.
public sealed record CourseStatusChangedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid CourseId { get; init; }
    public CourseStatus NewStatus { get; init; }

    public static CourseStatusChangedEvent Create(Guid courseId, CourseStatus newStatus) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            CourseId = courseId,
            NewStatus = newStatus
        };
}