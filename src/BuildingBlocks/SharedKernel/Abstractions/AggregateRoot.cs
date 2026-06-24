namespace SharedKernel.Abstractions;

/// Base class for all aggregate roots.
/// Extends Entity with the ability to collect domain events for dispatch via the Outbox Pattern
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// Read-only snapshot of pending domain events.
    /// Consumed by the Infrastructure layer before saving to the Outbox table.
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(Guid id) : base(id) { }

    protected AggregateRoot() { }

    /// Records a domain event to be dispatched after the current transaction commits.
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// Called by the Infrastructure layer after events have been written to the Outbox table.
    public void ClearDomainEvents() => _domainEvents.Clear();
}