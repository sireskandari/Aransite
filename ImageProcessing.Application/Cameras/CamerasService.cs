using Microsoft.EntityFrameworkCore;
using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Application.Common;
using ImageProcessing.Domain.Entities.Cameras;

namespace ImageProcessing.Application.Cameras;

public sealed class CamerasService(IAppDbContext db) : ICamerasService
{
    public async Task<CameraResponse> CreateAsync(CreateCameraRequest req, CancellationToken ct)
    {
        var Camera = new Camera
        {
            Key = req.Key.Trim(),
            Location = req.Location.Trim(),
            RTSP = req.RTSP.Trim(),
            CreatedUtc = DateTime.UtcNow,
            IsActive = req.IsActive,
        };

        db.Cameras.Add(Camera);
        await db.SaveChangesAsync(ct);

        return new CameraResponse(Camera.Id, Camera.Key!, Camera.Location!, Camera.RTSP!, Camera.IsActive, Camera.CreatedUtc);
    }
    public async Task<CameraResponse> UpdateAsync(Guid Id, UpdateCameraRequest req, CancellationToken ct)
    {
        var Camera = new Camera
        {
            Id = Id,
            Key = req.Key.Trim(),
            Location = req.Location.Trim(),
            RTSP = req.RTSP.Trim(),
            CreatedUtc = DateTime.UtcNow,
            IsActive = req.IsActive
        };

        db.Cameras.Update(Camera);
        await db.SaveChangesAsync(ct);

        return new CameraResponse(Camera.Id, Camera.Key!, Camera.Location!, Camera.RTSP!, Camera.IsActive, Camera.CreatedUtc);
    }

    public async Task<CameraResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Cameras
            .Where(u => u.Id == id)
            .Select(u => new CameraResponse(u.Id, u.Key!, u.Location!, u.RTSP!, u.IsActive, u.CreatedUtc))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<CameraResponse>> ListAsync(string? search, int pageNumber, int pageSize, CancellationToken ct)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

        var query = db.Cameras.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                (u.Key ?? "").Contains(s) ||
                (u.RTSP ?? "").Contains(s) ||
                (u.Location ?? "").Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new CameraResponse(u.Id, u.Key!, u.Location!, u.RTSP!, u.IsActive, u.CreatedUtc))
            .ToListAsync(ct);

        return new PagedResult<CameraResponse>(items, total, pageNumber, pageSize);
    }
    public async Task<List<CameraResponse>> GetAll(string? search, CancellationToken ct)
    {
        var query = db.Cameras.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                (u.Key ?? "").Contains(s) ||
                (u.RTSP ?? "").Contains(s) ||
                (u.Location ?? "").Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedUtc)
            .Select(u => new CameraResponse(u.Id, u.Key!, u.Location!, u.RTSP!,u.IsActive, u.CreatedUtc))
            .ToListAsync(ct);

        return items;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.Cameras.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.Cameras.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
