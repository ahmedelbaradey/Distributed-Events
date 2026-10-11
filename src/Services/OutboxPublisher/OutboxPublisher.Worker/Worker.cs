using System.Text;
using System.Text.Json;
using EventIngestion.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Shared.Contracts;

namespace OutboxPublisher.Worker;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly TimeSpan _leaseDuration = TimeSpan.FromMinutes(1);
    private readonly string _workerName = nameof(Worker);
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox publisher worker started.");

        await EnsureTopologyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EventIngestionDbContext>();

                var message = await ClaimNextMessageAsync(db, stoppingToken);
                if (message is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                await PublishMessageAsync(message, stoppingToken);
                await MarkMessagePublishedAsync(db, message.Id, stoppingToken);
                logger.LogInformation("Published outbox event {EventId} via RabbitMQ.", message.EventId);
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

    private async Task<OutboxMessage?> ClaimNextMessageAsync(EventIngestionDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var candidate = await db.OutboxMessages
            .Where(x => x.Status == "Pending" || (x.Status == "Processing" && (x.LockExpiresAt == null || x.LockExpiresAt <= now)))
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate is null)
        {
            return null;
        }

        var updatedRows = await db.OutboxMessages
            .Where(x => x.Id == candidate.Id &&
                (x.Status == "Pending" || (x.Status == "Processing" && (x.LockExpiresAt == null || x.LockExpiresAt <= now))))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "Processing")
                .SetProperty(x => x.LockedBy, _workerName)
                .SetProperty(x => x.LockExpiresAt, now.Add(_leaseDuration))
                .SetProperty(x => x.RetryCount, x => x.RetryCount + 1), cancellationToken);

        if (updatedRows == 0)
        {
            return null;
        }

        return await db.OutboxMessages
            .SingleAsync(x => x.Id == candidate.Id, cancellationToken);
    }

    private async Task PublishMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var route = EventRouting.RouteKey(
            Enum.TryParse<EventType>(message.EventType, out var eventType) ? eventType : EventType.AuditTrail,
            Severity.Medium);

        var payload = JsonSerializer.Serialize(new
        {
            message.EventId,
            message.EventType,
            message.Payload,
            message.CreatedAtUtc,
            route,
            CorrelationId = Guid.NewGuid().ToString("N")
        });

        var channel = await GetChannelAsync(cancellationToken);

        await channel.BasicPublishAsync(
            exchange: QueueNames.ExchangeName,
            routingKey: route,
            mandatory: true,
            basicProperties: new BasicProperties
            {
                Persistent = true,
                DeliveryMode = 2
            },
            body: Encoding.UTF8.GetBytes(payload),
            cancellationToken: cancellationToken);

        await channel.WaitForConfirmsOrDieAsync(TimeSpan.FromSeconds(10), cancellationToken);
    }

    private async Task MarkMessagePublishedAsync(EventIngestionDbContext db, long messageId, CancellationToken cancellationToken)
    {
        var publishedAt = DateTimeOffset.UtcNow;

        await db.OutboxMessages
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "Published")
                .SetProperty(x => x.PublishedAtUtc, publishedAt)
                .SetProperty(x => x.LockExpiresAt, null)
                .SetProperty(x => x.LockedBy, null)
                .SetProperty(x => x.LastError, null), cancellationToken);
    }

    private async Task EnsureTopologyAsync(CancellationToken cancellationToken)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            Port = 5672,
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest",
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: QueueNames.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var routes = new[]
        {
            (QueueNames.CriticalAlertsQueue, QueueNames.AlertCriticalRoutingKey, QueueNames.CriticalAlertsDlq, QueueNames.AlertCriticalDlqRoutingKey),
            (QueueNames.AnalyticsQueue, QueueNames.EventAnalyticsRoutingKey, QueueNames.AnalyticsDlq, QueueNames.EventAnalyticsDlqRoutingKey),
            (QueueNames.NotificationQueue, QueueNames.NotificationRoutingKey, QueueNames.NotificationDlq, QueueNames.NotificationDlqRoutingKey)
        };

        foreach (var (queueName, routingKey, dlqName, dlqRoutingKey) in routes)
        {
            await _channel.QueueDeclareAsync(
                queue: dlqName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = QueueNames.ExchangeName,
                    ["x-dead-letter-routing-key"] = dlqRoutingKey
                },
                cancellationToken: cancellationToken);

            await _channel.QueueBindAsync(
                queue: queueName,
                exchange: QueueNames.ExchangeName,
                routingKey: routingKey,
                cancellationToken: cancellationToken);

            await _channel.QueueBindAsync(
                queue: dlqName,
                exchange: QueueNames.ExchangeName,
                routingKey: dlqRoutingKey,
                cancellationToken: cancellationToken);
        }

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is null || _connection is null || _connection.IsOpen is false)
        {
            await EnsureTopologyAsync(cancellationToken);
        }

        return _channel!;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }
}
