using EventIngestion.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventIngestion.Infrastructure;

public sealed class EventIngestionDbContext : DbContext
{
    public EventIngestionDbContext(DbContextOptions<EventIngestionDbContext> options) : base(options)
    {
    }

    public DbSet<EventRecord> EventRecords => Set<EventRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventRecord>(entity =>
        {
            entity.ToTable("EventRecords");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.Property(x => x.SourceId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Payload).HasColumnType("jsonb");
            entity.Property(x => x.QueueRoute).HasMaxLength(64);
            entity.Property(x => x.ReceivedAtUtc).HasColumnType("timestamptz");
            entity.Property(x => x.OccurredAtUtc).HasColumnType("timestamptz");
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.LockExpiresAt);
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.Property(x => x.Payload).HasColumnType("jsonb");
            entity.Property(x => x.CreatedAtUtc).HasColumnType("timestamptz");
            entity.Property(x => x.PublishedAtUtc).HasColumnType("timestamptz");
            entity.Property(x => x.LockExpiresAt).HasColumnType("timestamptz");
        });
    }
}
