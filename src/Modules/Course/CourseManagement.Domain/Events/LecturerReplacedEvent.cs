using SharedKernel.Abstractions;

namespace CourseManagement.Domain.Events;

// Published when the lecturer responsible for a course is replaced.
public sealed record LecturerReplacedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid CourseId { get; init; }
    public Guid PreviousLecturerId { get; init; }
    public Guid NewLecturerId { get; init; }

    public static LecturerReplacedEvent Create(
        Guid courseId,
        Guid previousLecturerId,
        Guid newLecturerId) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            CourseId = courseId,
            PreviousLecturerId = previousLecturerId,
            NewLecturerId = newLecturerId
        };
}