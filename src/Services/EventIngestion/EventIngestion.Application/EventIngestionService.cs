using EventIngestion.Domain;
using Shared.Contracts;

namespace EventIngestion.Application;

public sealed class EventIngestionService : IEventIngestionService
{
    private readonly IEventStore _store;

    public EventIngestionService(IEventStore store)
    {
        _store = store;
    }

    public async Task<EventPersistenceResult> IngestAsync(EventSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SourceId))
        {
            return new EventPersistenceResult
            {
                Success = false,
                IsDuplicate = false,
                Message = "SourceId is required."
            };
        }

        var envelope = new EventEnvelope
        {
            EventId = Guid.NewGuid(),
            SourceId = request.SourceId,
            EventType = request.EventType,
            Severity = request.Severity,
            Payload = request.Payload,
            OccurredAtUtc = request.OccurredAtUtc ?? DateTimeOffset.UtcNow,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
            CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId
        };

        return await _store.SaveAsync(envelope, cancellationToken);
    }

    public Task<EventSummary> GetSummaryAsync(CancellationToken cancellationToken = default) => _store.GetSummaryAsync(cancellationToken);
}
