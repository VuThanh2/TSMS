using CourseManagement.Domain.ValueObjects;
using SharedKernel.Abstractions;

namespace CourseManagement.Domain.Events;

// Published khi Admin thêm 1 WeeklySlot mới — kéo theo việc sinh hàng loạt ClassSession
// từ StartDate đến EndDate của Course.
public sealed record WeeklySlotAddedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid CourseId { get; init; }
    public Guid WeeklySlotId { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public SessionType SessionType { get; init; }
    public int GeneratedSessionCount { get; init; }

    public static WeeklySlotAddedEvent Create(
        Guid courseId,
        Guid weeklySlotId,
        DayOfWeek dayOfWeek,
        SessionType sessionType,
        int generatedSessionCount) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            CourseId = courseId,
            WeeklySlotId = weeklySlotId,
            DayOfWeek = dayOfWeek,
            SessionType = sessionType,
            GeneratedSessionCount = generatedSessionCount
        };
}