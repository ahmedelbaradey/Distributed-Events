using Shared.Contracts;

namespace EventIngestion.Domain;

public sealed class EventRecord
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public Severity Severity { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;
    public string QueueRoute { get; set; } = string.Empty;

    public static EventRecord FromEnvelope(EventEnvelope envelope) => new()
    {
        EventId = envelope.EventId,
        SourceId = envelope.SourceId,
        EventType = envelope.EventType,
        Severity = envelope.Severity,
        Payload = envelope.Payload,
        OccurredAtUtc = envelope.OccurredAtUtc,
        ReceivedAtUtc = envelope.ReceivedAtUtc,
        CorrelationId = envelope.CorrelationId,
        QueueRoute = EventRouting.RouteKey(envelope.EventType, envelope.Severity)
    };
}

public sealed class OutboxMessage
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public string? LockedBy { get; set; }
    public DateTimeOffset? LockExpiresAt { get; set; }
}

public sealed class EventSummary
{
    public long TotalEvents { get; set; }
    public long CriticalAlerts { get; set; }
    public long AnalyticsEvents { get; set; }
}
