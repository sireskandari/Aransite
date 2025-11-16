using Microsoft.EntityFrameworkCore;
using ImageProcessing.Domain.Entities.Auth;
using ImageProcessing.Domain.Entities.Users;
using ImageProcessing.Domain.Entities.EdgeEvents;
using ImageProcessing.Domain.Entities.Cameras;
using ImageProcessing.Domain.Entities.DetectTargets;

namespace ImageProcessing.Application.Abstractions.Data;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Camera> Cameras { get; }
    DbSet<DetectTarget> DetectTargets{ get; }
    DbSet<EdgeEvent> EdgeEvents { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}