using Microsoft.EntityFrameworkCore;
using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Application.Common;
using ImageProcessing.Application.Timelapses;
using ImageProcessing.Domain.Entities.Timelapse;

namespace ImageProcessing.Application.Timelapse;

public sealed class TimelapseService(IAppDbContext db) : ITimelapseService
{
    public async Task<TimelapseResponse> CreateAsync(CreateTimelapseRequest req, CancellationToken ct)
    {
        var Timelapse = new Domain.Entities.Timelapse.Timelapse
        {
            FileFormat = req.FileFormat.Trim(),
            FilePath = req.FilePath.Trim(),
            FileSize = req.FileSize.Trim(),
            CreatedUtc = DateTime.UtcNow,
            Status = req.Status,
            ErrorMessage = req.ErrorMessage,
        };

        db.Timelapse.Add(Timelapse);
        await db.SaveChangesAsync(ct);

        return new TimelapseResponse(Timelapse.Id, Timelapse.FilePath!, Timelapse.FileFormat!, Timelapse.FileSize!, Timelapse.Status, Timelapse.ErrorMessage!, Timelapse.CreatedUtc);
    }
    public async Task<TimelapseResponse> UpdateAsync(Guid id, UpdateTimelapseRequest req, CancellationToken ct)
    {
        var entity = await db.Timelapse
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity == null)
            throw new KeyNotFoundException($"Timelapse {id} not found.");

        if (req.FileFormat != null)
            entity.FileFormat = req.FileFormat.Trim();

        if (req.FilePath != null)
            entity.FilePath = req.FilePath.Trim();

        if (req.FileSize != null)
            entity.FileSize = req.FileSize.Trim();

        entity.Status = req.Status;
        entity.ErrorMessage = req.ErrorMessage;

        await db.SaveChangesAsync(ct);

        return new TimelapseResponse(
            entity.Id,
            entity.FilePath!,
            entity.FileFormat!,
            entity.FileSize!,
            entity.Status,
            entity.ErrorMessage,
            entity.CreatedUtc);
    }


    public async Task<TimelapseResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Timelapse
            .Where(u => u.Id == id)
            .Select(u => new TimelapseResponse(u.Id, u.FilePath!, u.FileFormat!, u.FileSize!, u.Status, u.ErrorMessage!, u.CreatedUtc))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<TimelapseResponse>> ListAsync(string? search, int pageNumber, int pageSize, CancellationToken ct)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

        var query = db.Timelapse.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                (u.FilePath ?? "").Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new TimelapseResponse(u.Id, u.FilePath!, u.FileFormat!, u.FileSize!, u.Status, u.ErrorMessage!, u.CreatedUtc))
            .ToListAsync(ct);

        return new PagedResult<TimelapseResponse>(items, total, pageNumber, pageSize);
    }
    public async Task<List<TimelapseResponse>> GetAll(string? search, CancellationToken ct)
    {
        var query = db.Timelapse.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                (u.FilePath ?? "").Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedUtc)
            .Select(u => new TimelapseResponse(u.Id, u.FilePath!, u.FileFormat!, u.FileSize!, u.Status, u.ErrorMessage!, u.CreatedUtc))
            .ToListAsync(ct);

        return items;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.Timelapse.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.Timelapse.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
