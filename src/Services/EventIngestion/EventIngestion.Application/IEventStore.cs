using EventIngestion.Domain;
using Shared.Contracts;

namespace EventIngestion.Application;

public interface IEventStore
{
    Task<EventPersistenceResult> SaveAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
    Task<EventSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<long> CountAsync(CancellationToken cancellationToken = default);
}

public interface IEventIngestionService
{
    Task<EventPersistenceResult> IngestAsync(EventSubmissionRequest request, CancellationToken cancellationToken = default);
    Task<EventSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}
