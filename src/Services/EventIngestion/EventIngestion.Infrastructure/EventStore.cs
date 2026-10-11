using EventIngestion.Application;
using EventIngestion.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace EventIngestion.Infrastructure;

public sealed class EventStore : IEventStore
{
    private readonly EventIngestionDbContext _db;

    public EventStore(EventIngestionDbContext db)
    {
        _db = db;
    }

    public async Task<EventPersistenceResult> SaveAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (await _db.EventRecords.AnyAsync(x => x.EventId == envelope.EventId, cancellationToken))
        {
            return new EventPersistenceResult
            {
                EventId = envelope.EventId,
                IsDuplicate = true,
                Success = true,
                Message = "Duplicate event ignored."
            };
        }

        var record = EventRecord.FromEnvelope(envelope);
        var outbox = new OutboxMessage
        {
            EventId = envelope.EventId,
            EventType = envelope.EventType.ToString(),
            Payload = envelope.Payload,
            Status = "Pending",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastError = null
        };

        _db.EventRecords.Add(record);
        _db.OutboxMessages.Add(outbox);
        await _db.SaveChangesAsync(cancellationToken);

        return new EventPersistenceResult
        {
            EventId = envelope.EventId,
            IsDuplicate = false,
            Success = true,
            Message = "Event stored successfully."
        };
    }

    public async Task<EventSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var total = await _db.EventRecords.LongCountAsync(cancellationToken);
        var critical = await _db.EventRecords.CountAsync(x => x.Severity == Severity.Critical || x.EventType == EventType.MalwareDetected, cancellationToken);
        var analytics = await _db.EventRecords.CountAsync(x => x.QueueRoute == QueueNames.EventAnalyticsRoutingKey, cancellationToken);

        return new EventSummary { TotalEvents = total, CriticalAlerts = critical, AnalyticsEvents = analytics };
    }

    public Task<long> CountAsync(CancellationToken cancellationToken = default) => _db.EventRecords.LongCountAsync(cancellationToken);
}
