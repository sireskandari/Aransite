using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Domain.Entities.Auth;
using ImageProcessing.Domain.Entities.Cameras;
using ImageProcessing.Domain.Entities.DetectTargets;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.Entity<LogEvent>(entity =>
        {
            entity.ToTable("serilog_logs", "dbo");
            entity.HasKey(x => x.Id);
        });
    }
}
