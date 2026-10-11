using EventIngestion.Application;
using EventIngestion.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Xunit;

namespace IntegrationTests;

public class EventIngestionIntegrationTests
{
    [Fact]
    public async Task IngestAsync_PersistsEventAndOutboxEntry()
    {
        var options = new DbContextOptionsBuilder<EventIngestionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new EventIngestionDbContext(options);
        var service = new EventIngestionService(new EventStore(db));

        var result = await service.IngestAsync(new EventSubmissionRequest
        {
            SourceId = "integration-test",
            EventType = EventType.DeviceAnomaly,
            Severity = Severity.High,
            Payload = "{\"detected\":true}",
            CorrelationId = "corr-42"
        });

        Assert.True(result.Success);
        Assert.Equal(1, await db.EventRecords.CountAsync());
        Assert.Equal(1, await db.OutboxMessages.CountAsync());
    }
}
