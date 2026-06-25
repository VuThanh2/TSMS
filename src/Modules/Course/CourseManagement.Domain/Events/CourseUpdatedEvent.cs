using SharedKernel.Abstractions;

namespace CourseManagement.Domain.Events;

// Published when course info (name, description, endDate, maxCapacity) is updated.
public sealed record CourseUpdatedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public DateOnly EndDate { get; init; }
    public int MaxCapacity { get; init; }

    public static CourseUpdatedEvent Create(
        Guid courseId,
        string courseName,
        DateOnly endDate,
        int maxCapacity) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            CourseId = courseId,
            CourseName = courseName,
            EndDate = endDate,
            MaxCapacity = maxCapacity
        };
}