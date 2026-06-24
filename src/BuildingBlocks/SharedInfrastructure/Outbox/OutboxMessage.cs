namespace SharedInfrastructure.Outbox;

/// Written atomically within the same transaction as the domain operation.
/// Hangfire picks up unprocessed messages and dispatches them to their handlers.
public sealed class OutboxMessage {
    public Guid Id { get; private set; }

    /// Fully-qualified type name of the original domain event.
    /// Used to deserialize Payload back to the correct IDomainEvent type.
    public string Type { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTime OccurredOn { get; private set; }

    /// Null = pending dispatch. Set when Hangfire successfully processes this message.
    public DateTime? ProcessedOn { get; private set; }

    /// Populated when dispatch fails. Null on success or when not yet attempted.
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(Guid id, string type, string payload, DateTime occurredOn) {
        return new OutboxMessage {
            Id = id,
            Type = type,
            Payload = payload,
            OccurredOn = occurredOn
        };
    }

    public void MarkProcessed(DateTime processedOn) {
        ProcessedOn = processedOn;
        Error = null;
    }

    public void MarkFailed(string error) {
        Error = error;
    }
}