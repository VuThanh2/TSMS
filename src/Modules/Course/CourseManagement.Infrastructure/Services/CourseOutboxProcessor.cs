using System.Text.Json;
using CourseManagement.Infrastructure.Persistence;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CourseManagement.Infrastructure.Services;

// Hangfire recurring job — đọc OutboxMessages của CourseManagement BC,
// deserialize thành đúng IDomainEvent, publish qua IPublisher (MediatR in-process bus).
public class CourseOutboxProcessor {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private readonly CourseDbContext _context;
    private readonly IPublisher _publisher;
    private readonly ILogger<CourseOutboxProcessor> _logger;

    public CourseOutboxProcessor(
        CourseDbContext context,
        IPublisher publisher,
        ILogger<CourseOutboxProcessor> logger) {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default) {
        var messages = await _context.OutboxMessages
            .Where(m => m.ProcessedOn == null)
            .OrderBy(m => m.OccurredOn)
            .ToListAsync(cancellationToken);

        foreach (var message in messages) {
            try {
                var eventType = Type.GetType(message.Type);

                if (eventType is null) {
                    _logger.LogWarning(
                        "Cannot resolve type '{Type}' for OutboxMessage {Id}. Skipping.",
                        message.Type, message.Id);
                    message.MarkProcessed(DateTime.UtcNow);
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType, JsonOptions)
                    as MediatR.INotification;

                if (domainEvent is null) {
                    _logger.LogWarning(
                        "Cannot deserialize OutboxMessage {Id} as INotification. Skipping.",
                        message.Id);
                    message.MarkProcessed(DateTime.UtcNow);
                    continue;
                }

                await _publisher.Publish(domainEvent, cancellationToken);
                message.MarkProcessed(DateTime.UtcNow);

                _logger.LogInformation(
                    "Processed Course OutboxMessage {Id}, type {Type}.",
                    message.Id, eventType.Name);
            } catch (Exception ex) {
                message.MarkFailed(ex.Message);
                _logger.LogError(ex,
                    "Failed to process Course OutboxMessage {Id}.", message.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}