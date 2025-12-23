using HeroLog.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace HeroLog.Worker.Data;

/// <summary>
/// Entity Framework Core DbContext for logging data
/// </summary>
public class LogDbContext : DbContext
{
    public LogDbContext(DbContextOptions<LogDbContext> options) : base(options)
    {
    }

    public DbSet<ServiceLogEntity> ServiceLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ServiceLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ServiceName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LogLevel).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Source).IsRequired().HasMaxLength(200);
            
            // Create index on Timestamp for better query performance
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.ServiceName);
            entity.HasIndex(e => e.LogLevel);
        });
    }
}
