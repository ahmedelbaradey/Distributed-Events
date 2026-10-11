using EventIngestion.Infrastructure;
using Microsoft.EntityFrameworkCore;
using OutboxPublisher.Worker;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("EVENT_INGESTION_CONNECTION")
    ?? "Host=localhost;Port=5432;Database=event_ingestion;Username=postgres;Password=postgres";

builder.Services.AddDbContext<EventIngestionDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
