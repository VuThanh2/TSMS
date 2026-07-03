using SharedKernel.Abstractions;

namespace CourseManagement.Domain.Events;

// Published khi 1 buổi học cụ thể bị hủy (soft-cancel, không xóa vật lý) — do Admin chủ động
// hủy 1 buổi (vd nghỉ lễ) hoặc do rút ngắn EndDate của Course kéo theo hủy các buổi vượt quá.
public sealed record ClassSessionCancelledEvent : IDomainEvent {
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public Guid CourseId { get; init; }
    public Guid ClassSessionId { get; init; }

    public static ClassSessionCancelledEvent Create(Guid courseId, Guid classSessionId) =>
        new() {
            EventId = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            CourseId = courseId,
            ClassSessionId = classSessionId
        };
}