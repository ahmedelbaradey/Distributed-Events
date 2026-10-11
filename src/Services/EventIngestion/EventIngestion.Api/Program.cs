using EventIngestion.Application;
using EventIngestion.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=postgres;Port=5432;Database=event_ingestion;Username=postgres;Password=postgres";
builder.Services.AddDbContext<EventIngestionDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IEventStore, EventStore>();
builder.Services.AddScoped<IEventIngestionService, EventIngestionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapPost("/api/events", async (EventSubmissionRequest request, IEventIngestionService service) =>
{
    var result = await service.IngestAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});
app.MapGet("/api/events/summary", async (IEventIngestionService service) => Results.Ok(await service.GetSummaryAsync()));
app.MapPost("/api/simulator/start", () => Results.Ok(new { status = "started" }));
app.MapPost("/api/simulator/stop", () => Results.Ok(new { status = "stopped" }));
app.MapPut("/api/simulator/rate", (int rate) => Results.Ok(new { rate }));
app.MapGet("/api/simulator/status", () => Results.Ok(new { running = true, rate = 1000 }));
app.MapGet("/api/metrics/summary", () => Results.Ok(new { generated = 0, accepted = 0, published = 0, processed = 0 }));
app.MapGet("/api/queues/status", () => Results.Ok(new[]
{
    new QueueStatusResponse { QueueName = QueueNames.CriticalAlertsQueue, Depth = 0, ConsumerCount = 1, OldestMessageAge = TimeSpan.Zero, DeadLetterQueueCount = 0 },
    new QueueStatusResponse { QueueName = QueueNames.AnalyticsQueue, Depth = 0, ConsumerCount = 1, OldestMessageAge = TimeSpan.Zero, DeadLetterQueueCount = 0 }
}));
app.MapGet("/api/outbox/status", () => Results.Ok(new { pending = 0, published = 0, failed = 0 }));
app.MapGet("/api/alerts", () => Results.Ok(Array.Empty<object>()));
app.MapPost("/api/faults/database-delay", () => Results.Ok(new { status = "configured" }));
app.MapPost("/api/faults/notification-outage", () => Results.Ok(new { status = "configured" }));
app.MapPost("/api/faults/publisher-delay", () => Results.Ok(new { status = "configured" }));
app.MapPost("/api/faults/reset", () => Results.Ok(new { status = "reset" }));

app.Run();
