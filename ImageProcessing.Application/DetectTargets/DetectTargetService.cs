using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Application.DetectTargets;
using ImageProcessing.Application.Common;
using ImageProcessing.Domain.Entities.DetectTargets;
using Microsoft.EntityFrameworkCore;

namespace ImageProcessing.Application.DetectTargets;

public sealed class DetectTargetsService(IAppDbContext db) : IDetectTargetsService
{
    public async Task<DetectTargetResponse> CreateAsync(CreateDetectTargetRequest req, CancellationToken ct)
    {
        var DetectTarget = new DetectTarget
        {
            CameraKey = req.CameraKey.Trim(),
            Targets = req.Targets.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        db.DetectTargets.Add(DetectTarget);
        await db.SaveChangesAsync(ct);

        return new DetectTargetResponse(DetectTarget.Id, DetectTarget.CameraKey!, DetectTarget.Targets!, DetectTarget.CreatedUtc);
    }
    public async Task<DetectTargetResponse> UpdateAsync(Guid Id, UpdateDetectTargetRequest req, CancellationToken ct)
    {
        var DetectTarget = new DetectTarget
        {
            Id = Id,
            CameraKey = req.CameraKey.Trim(),
            Targets = req.Targets.Trim(),
        };

        db.DetectTargets.Update(DetectTarget);
        await db.SaveChangesAsync(ct);

        return new DetectTargetResponse(DetectTarget.Id, DetectTarget.CameraKey!, DetectTarget.Targets!, DetectTarget.CreatedUtc);
    }

    public async Task<DetectTargetResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.DetectTargets
            .Where(u => u.Id == id)
            .Select(u => new DetectTargetResponse(u.Id, u.CameraKey!, u.Targets!, u.CreatedUtc))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<DetectTargetResponse>> GetAll(string? search, CancellationToken ct)
    {
        var query = db.DetectTargets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                (u.CameraKey ?? "").Contains(s) ||
                (u.Targets ?? "").Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedUtc)
            .Select(u => new DetectTargetResponse(u.Id, u.CameraKey!, u.Targets!, u.CreatedUtc))
            .ToListAsync(ct);

        return items;
    }

    public async Task<PagedResult<DetectTargetResponse>> ListAsync(string? search, int pageNumber, int pageSize, CancellationToken ct)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

        var query = db.DetectTargets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                (u.CameraKey ?? "").Contains(s) ||
                (u.Targets ?? "").Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new DetectTargetResponse(u.Id, u.CameraKey!, u.Targets!, u.CreatedUtc))
            .ToListAsync(ct);

        return new PagedResult<DetectTargetResponse>(items, total, pageNumber, pageSize);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.DetectTargets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.DetectTargets.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
