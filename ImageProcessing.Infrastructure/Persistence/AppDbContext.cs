using Microsoft.EntityFrameworkCore;
using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Domain.Entities.Auth;
using ImageProcessing.Domain.Entities.EdgeEvents;
using ImageProcessing.Domain.Entities.Users;
using ImageProcessing.Domain.Entities.Cameras;
using ImageProcessing.Domain.Entities.DetectTargets;

namespace ImageProcessing.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<DetectTarget> DetectTargets => Set<DetectTarget>();
    public DbSet<EdgeEvent> EdgeEvents => Set<EdgeEvent>();
    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
