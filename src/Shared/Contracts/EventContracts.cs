namespace Shared.Contracts;

public enum EventType
{
    AuthenticationFailure,
    AccessDenied,
    DeviceAnomaly,
    MalwareDetected,
    AuditTrail,
    SystemHealth
}

public enum Severity
{
    Low,
    Medium,
    High,
    Critical
}

public enum EventRoute
{
    Critical,
    Analytics,
    Notification,
    Unspecified
}

public static class EventRouting
{
    public static string RouteKey(EventType eventType, Severity severity)
    {
        if (eventType is EventType.MalwareDetected or EventType.AuthenticationFailure || severity == Severity.Critical)
        {
            return QueueNames.AlertCriticalRoutingKey;
        }

        if (eventType is EventType.SystemHealth or EventType.AuditTrail)
        {
            return QueueNames.NotificationRoutingKey;
        }

        return QueueNames.EventAnalyticsRoutingKey;
    }

    public static EventRoute Route(EventType eventType, Severity severity)
    {
        if (eventType is EventType.MalwareDetected or EventType.AuthenticationFailure || severity == Severity.Critical)
        {
            return EventRoute.Critical;
        }

        if (eventType is EventType.SystemHealth or EventType.AuditTrail)
        {
            return EventRoute.Notification;
        }

        return EventRoute.Analytics;
    }
}

public sealed class EventEnvelope
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string SourceId { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public Severity Severity { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
}

public sealed class EventSubmissionRequest
{
    public string SourceId { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public Severity Severity { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTimeOffset? OccurredAtUtc { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
}

public sealed class EventPersistenceResult
{
    public Guid EventId { get; set; }
    public bool IsDuplicate { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class OutboxMessageRecord
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

public sealed class QueueStatusResponse
{
    public string QueueName { get; set; } = string.Empty;
    public long Depth { get; set; }
    public long ConsumerCount { get; set; }
    public TimeSpan? OldestMessageAge { get; set; }
    public long DeadLetterQueueCount { get; set; }
}

public static class QueueNames
{
    public const string ExchangeName = "camera-events";
    public const string CriticalAlertsQueue = "critical-alerts";
    public const string AnalyticsQueue = "analytics-events";
    public const string NotificationQueue = "notification-events";
    public const string CriticalAlertsDlq = "critical-alerts-dlq";
    public const string AnalyticsDlq = "analytics-events-dlq";
    public const string NotificationDlq = "notification-events-dlq";
    public const string AlertCriticalRoutingKey = "alert.critical";
    public const string EventAnalyticsRoutingKey = "event.analytics";
    public const string NotificationRoutingKey = "event.notification";
    public const string AlertCriticalDlqRoutingKey = "alert.critical.dlq";
    public const string EventAnalyticsDlqRoutingKey = "event.analytics.dlq";
    public const string NotificationDlqRoutingKey = "event.notification.dlq";
}
