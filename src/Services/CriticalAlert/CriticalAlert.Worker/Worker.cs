using EventIngestion.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace CriticalAlert.Worker;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Critical alert worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EventIngestionDbContext>();

                var alerts = await db.EventRecords
                    .Where(x => x.QueueRoute == QueueNames.AlertCriticalRoutingKey)
                    .OrderByDescending(x => x.ReceivedAtUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                if (alerts.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                foreach (var alert in alerts)
                {
                    logger.LogCritical(
                        "Critical alert triggered: {EventId} from {SourceId} type {EventType} severity {Severity}",
                        alert.EventId,
                        alert.SourceId,
                        alert.EventType,
                        alert.Severity);
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Critical alert worker loop failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
