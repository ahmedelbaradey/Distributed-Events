using EventIngestion.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace OutboxPublisher.Worker;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EventIngestionDbContext>();

                var dueMessages = await db.OutboxMessages
                    .Where(x => x.Status == "Pending")
                    .OrderBy(x => x.CreatedAtUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (dueMessages.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                foreach (var message in dueMessages)
                {
                    var route = EventRouting.RouteKey(
                        Enum.TryParse<EventType>(message.EventType, out var eventType) ? eventType : EventType.AuditTrail,
                        Severity.Medium);

                    logger.LogInformation(
                        "Publishing outbox message {EventId} to route {Route}",
                        message.EventId,
                        route);

                    message.Status = "Published";
                    message.PublishedAtUtc = DateTimeOffset.UtcNow;
                    message.LockedBy = nameof(Worker);
                    message.LockExpiresAt = null;
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox publish loop failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
