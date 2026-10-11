using EventIngestion.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Notification.Worker;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EventIngestionDbContext>();

                var notifications = await db.EventRecords
                    .OrderByDescending(x => x.ReceivedAtUtc)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                if (notifications.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                    continue;
                }

                foreach (var item in notifications)
                {
                    logger.LogInformation(
                        "Notification dispatched for event {EventId} from {SourceId}",
                        item.EventId,
                        item.SourceId);
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification worker loop failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
