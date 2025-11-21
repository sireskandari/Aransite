using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Domain.Entities.Auth;
using ImageProcessing.Domain.Entities.Cameras;
using ImageProcessing.Domain.Entities.DetectTargets;
using ImageProcessing.Domain.Entities.EdgeDevices;
using ImageProcessing.Domain.Entities.EdgeEvents;
using ImageProcessing.Domain.Entities.LogEvents;
using ImageProcessing.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace ImageProcessing.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<DetectTarget> DetectTargets => Set<DetectTarget>();
    public DbSet<EdgeEvent> EdgeEvents => Set<EdgeEvent>();
    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LogEvent> LogEvents => Set<LogEvent>();
    public DbSet<EdgeDevice> EdgeDevices => Set<EdgeDevice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.Entity<LogEvent>(b =>
        {
            b.ToTable("serilog_logs");

            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("id");

            b.Property(x => x.Timestamp).HasColumnName("Timestamp");
            b.Property(x => x.Level).HasColumnName("Level");
            b.Property(x => x.Template).HasColumnName("Template");
            b.Property(x => x.Message).HasColumnName("Message");
            b.Property(x => x.Exception).HasColumnName("Exception");
            b.Property(x => x.Properties).HasColumnName("Properties");

            b.Property(x => x.Ts).HasColumnName("_ts");
        });
    }
}
