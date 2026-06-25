using CourseManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;

namespace CourseManagement.Domain.Events;

// Published when a new class session is added to a course.
public sealed record ClassSessionAddedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid CourseId { get; init; }
    public Guid ClassSessionId { get; init; }
    public DateOnly SessionDate { get; init; }
    public SessionType SessionType { get; init; }

    public static ClassSessionAddedEvent Create(
        Guid courseId,
        Guid classSessionId,
        DateOnly sessionDate,
        SessionType sessionType) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            CourseId = courseId,
            ClassSessionId = classSessionId,
            SessionDate = sessionDate,
            SessionType = sessionType
        };
}