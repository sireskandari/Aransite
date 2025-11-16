using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Application.EdgeEvents;
using ImageProcessing.Application.Common;
using ImageProcessing.Application.EdgeEvents;
using ImageProcessing.Application.EdgeEvents;
using ImageProcessing.Domain.Entities.EdgeEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


public sealed class EdgeEventsService(IAppDbContext db) : IEdgeEventsService
{
    public async Task<EdgeEventsResponse> CreateAsync(CreateEdgeEventsRequest req, CancellationToken ct)
    {
        var EdgeEvents = new EdgeEvent
        {
            CameraId = req.CameraId.Trim(),
            ComputeInferenceMs = req.ComputeInferenceMs,
            ComputeModel = req.ComputeModel,
            Detections = req.Detections,
            FrameAnnotatedUrl = req.FrameAnnotatedUrl,
            FrameRawUrl = req.FrameRawUrl,
            ImageHeight = req.ImageHeight,
            ImageWidth = req.ImageWidth,
            CaptureTimestampUtc = req.CaptureTimestampUtc,
            CreatedUtc = DateTime.UtcNow
        };

        db.EdgeEvents.Add(EdgeEvents);
        await db.SaveChangesAsync(ct);

        return new EdgeEventsResponse(EdgeEvents.Id, EdgeEvents.CaptureTimestampUtc!, EdgeEvents.CreatedUtc, EdgeEvents.CameraId!, EdgeEvents.ComputeModel, EdgeEvents.ComputeInferenceMs, EdgeEvents.ImageWidth ?? 0, EdgeEvents.ImageHeight ?? 0, EdgeEvents.Detections, EdgeEvents.FrameRawUrl, EdgeEvents.FrameAnnotatedUrl);
    }

    public async Task<EdgeEventsResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.EdgeEvents
            .Where(u => u.Id == id)
            .Select(EdgeEvents => new EdgeEventsResponse(EdgeEvents.Id, EdgeEvents.CaptureTimestampUtc!, EdgeEvents.CreatedUtc, EdgeEvents.CameraId!, EdgeEvents.ComputeModel, EdgeEvents.ComputeInferenceMs, EdgeEvents.ImageWidth ?? 0, EdgeEvents.ImageHeight ?? 0, EdgeEvents.Detections, EdgeEvents.FrameRawUrl, EdgeEvents.FrameAnnotatedUrl))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<EdgeEventsResponse>> ListAsync(string? search, int pageNumber, int pageSize, CancellationToken ct)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

        var query = db.EdgeEvents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                (u.CameraId ?? "").Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(EdgeEvents => new EdgeEventsResponse(EdgeEvents.Id, EdgeEvents.CaptureTimestampUtc!, EdgeEvents.CreatedUtc, EdgeEvents.CameraId!, EdgeEvents.ComputeModel, EdgeEvents.ComputeInferenceMs, EdgeEvents.ImageWidth ?? 0, EdgeEvents.ImageHeight ?? 0, EdgeEvents.Detections, EdgeEvents.FrameRawUrl, EdgeEvents.FrameAnnotatedUrl))
            .ToListAsync(ct);

        return new PagedResult<EdgeEventsResponse>(items, total, pageNumber, pageSize);
    }

<<<<<<< HEAD
    public async Task<List<EdgeEventsResponse>> GetAll(string? search, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct)
=======
    public async Task<List<EdgeEventsResponse>> GetAll(string? search, CancellationToken ct)
>>>>>>> b186aa7 (v4)
    {
        var query = db.EdgeEvents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                (u.CameraId ?? "").Contains(s) ||
                (u.FrameAnnotatedUrl ?? "").Contains(s) ||
                (u.FrameRawUrl ?? "").Contains(s));
        }
<<<<<<< HEAD
        if (fromUtc.HasValue)
            query = query.Where(e => e.CaptureTimestampUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(e => e.CaptureTimestampUtc <= toUtc.Value);
=======
>>>>>>> b186aa7 (v4)

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedUtc)
            .Select(EdgeEvents => new EdgeEventsResponse(EdgeEvents.Id, EdgeEvents.CaptureTimestampUtc!, EdgeEvents.CreatedUtc, EdgeEvents.CameraId!, EdgeEvents.ComputeModel, EdgeEvents.ComputeInferenceMs, EdgeEvents.ImageWidth ?? 0, EdgeEvents.ImageHeight ?? 0, EdgeEvents.Detections, EdgeEvents.FrameRawUrl, EdgeEvents.FrameAnnotatedUrl))
            .ToListAsync(ct);

        return items;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.EdgeEvents.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.EdgeEvents.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
