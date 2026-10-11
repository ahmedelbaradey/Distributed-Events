using EventIngestion.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace Analytics.Worker;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Analytics worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EventIngestionDbContext>();

                var recent = await db.EventRecords
                    .Where(x => x.QueueRoute == QueueNames.EventAnalyticsRoutingKey)
                    .OrderByDescending(x => x.ReceivedAtUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                if (recent.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                    continue;
                }

                foreach (var record in recent)
                {
                    logger.LogInformation(
                        "Analytics processed event {EventId} from {SourceId} severity {Severity}",
                        record.EventId,
                        record.SourceId,
                        record.Severity);
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Analytics worker loop failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
