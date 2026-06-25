using SharedKernel.Abstractions;

namespace CourseManagement.Domain.Events;

// Published when a new course is created.
public sealed record CourseCreatedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid CourseId { get; init; }
    public Guid LecturerId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int MaxCapacity { get; init; }

    public static CourseCreatedEvent Create(
        Guid courseId,
        Guid lecturerId,
        string courseName,
        DateOnly startDate,
        DateOnly endDate,
        int maxCapacity) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            CourseId = courseId,
            LecturerId = lecturerId,
            CourseName = courseName,
            StartDate = startDate,
            EndDate = endDate,
            MaxCapacity = maxCapacity
        };
}