namespace SharedInfrastructure.Outbox;

/// Contract for persisting OutboxMessages within a BC's own DbContext transaction.
public interface IOutboxRepository {
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// Returns all unprocessed messages ordered by OccurredOn ascending.
    /// Called by the Hangfire processor job to fetch pending dispatches.
    Task<List<OutboxMessage>> GetPendingAsync(CancellationToken cancellationToken = default);
}