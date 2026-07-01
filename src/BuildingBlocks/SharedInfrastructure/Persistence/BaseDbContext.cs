using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Abstractions;
using SharedInfrastructure.Outbox;

namespace SharedInfrastructure.Persistence;

/// Intercepts SaveChangesAsync to collect domain events from tracked AggregateRoots
/// and persist them as OutboxMessages within the same transaction.
public abstract class BaseDbContext : DbContext, IUnitOfWork {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = false
    };

    protected BaseDbContext(DbContextOptions options) : base(options) { }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
        PublishDomainEventsToOutbox();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// Scans all tracked AggregateRoots for pending domain events,
    /// converts each to an OutboxMessage, adds them to the Outbox DbSet,
    /// then clears the events from the aggregate.
    private void PublishDomainEventsToOutbox() {
        var aggregates = ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates) {
            foreach (var domainEvent in aggregate.DomainEvents) {
                var type = domainEvent.GetType().AssemblyQualifiedName!;
                var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);

                var message = OutboxMessage.Create(
                    id: domainEvent.EventId,
                    type: type,
                    payload: payload,
                    occurredOn: domainEvent.OccurredOn);

                AddOutboxMessage(message);
            }

            aggregate.ClearDomainEvents();
        }
    }

    /// Each BC's DbContext overrides this to add the message to its own OutboxMessages DbSet.
    protected abstract void AddOutboxMessage(OutboxMessage message);
}