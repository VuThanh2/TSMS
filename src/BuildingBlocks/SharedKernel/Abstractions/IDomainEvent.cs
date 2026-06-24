namespace SharedKernel.Abstractions;

public interface IDomainEvent
{
    Guid EventId { get; init; }
    DateTime OccurredOn { get; init; }
}