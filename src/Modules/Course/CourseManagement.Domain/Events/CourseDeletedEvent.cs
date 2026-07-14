using SharedKernel.Abstractions;

namespace CourseManagement.Domain.Events;

// Published khi 1 course bị xóa (chỉ xảy ra với course Upcoming, chưa có Student enroll).
// Reporting BC lắng nghe để dọn CourseStatisticsView + ScoreDistribution rows tương ứng,
// tránh để lại projection mồ côi trong màn hình thống kê.
public sealed record CourseDeletedEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid CourseId { get; init; }

    public static CourseDeletedEvent Create(Guid courseId) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            CourseId = courseId
        };
}
