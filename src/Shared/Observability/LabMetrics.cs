using System.Diagnostics.Metrics;

namespace Shared.Observability;

public sealed class LabMetrics
{
    private readonly Meter _meter = new("DistributedEventsLab");
    public Counter<long> GeneratedEvents { get; } = new Meter("DistributedEventsLab").CreateCounter<long>("lab.generated.events");
    public Counter<long> AcceptedEvents { get; } = new Meter("DistributedEventsLab").CreateCounter<long>("lab.accepted.events");
    public Counter<long> PublishedEvents { get; } = new Meter("DistributedEventsLab").CreateCounter<long>("lab.published.events");
    public Counter<long> ProcessedEvents { get; } = new Meter("DistributedEventsLab").CreateCounter<long>("lab.processed.events");
    public Counter<long> DeliveredNotifications { get; } = new Meter("DistributedEventsLab").CreateCounter<long>("lab.notifications.delivered");
}
