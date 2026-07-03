using SharedKernel.Abstractions;

namespace CourseManagement.Domain.Events;

// Published khi Admin xóa 1 WeeklySlot — các ClassSession TƯƠNG LAI thuộc slot này bị hủy theo
// (buổi đã qua được giữ nguyên để bảo toàn lịch sử điểm danh).
public sealed record WeeklySlotRemovedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid CourseId { get; init; }
    public Guid WeeklySlotId { get; init; }
    public IReadOnlyList<Guid> RemovedFutureClassSessionIds { get; init; } = [];

    public static WeeklySlotRemovedEvent Create(
        Guid courseId,
        Guid weeklySlotId,
        IReadOnlyList<Guid> removedFutureClassSessionIds) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            CourseId = courseId,
            WeeklySlotId = weeklySlotId,
            RemovedFutureClassSessionIds = removedFutureClassSessionIds
        };
}