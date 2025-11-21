using Microsoft.EntityFrameworkCore;
using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Application.Common;
using ImageProcessing.Domain.Entities.EdgeDevices;

namespace ImageProcessing.Application.EdgeDevices;

public sealed class EdgeDevicesService(IAppDbContext db) : IEdgeDevicesService
{
    public async Task<EdgeDeviceResponse> CreateAsync(CreateEdgeDeviceRequest req, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        var device = await db.EdgeDevices
            .FirstOrDefaultAsync(d => d.DeviceId == req.DeviceId, ct);

        if (device == null)
        {
            device = new EdgeDevice
            {
                DeviceId = req.DeviceId,
                FirstSeenUtc = nowUtc
            };
            db.EdgeDevices.Add(device);
        }

        device.Hostname = req.Hostname ?? device.Hostname;
        device.LastHeartbeatUtc = nowUtc;
        device.LocalIp = req.LocalIp;
        device.CaptureSinceUtc = req.CaptureSinceUtc ?? device.CaptureSinceUtc;
        device.LastCaptureUtc = req.LastCaptureUtc ?? device.LastCaptureUtc;
        device.AppVersion = req.AppVersion ?? device.AppVersion;
        device.Status = req.Status ?? device.Status ?? "ok";

        await db.SaveChangesAsync(ct);

        return new EdgeDeviceResponse(device.Id, device.DeviceId, device.Hostname!, device.LocalIp!,
            device.CaptureSinceUtc, device.LastCaptureUtc, device.AppVersion, device.Status);
    }

    public async Task<EdgeDeviceResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.EdgeDevices
            .Where(u => u.Id == id)
            .Select(u => new EdgeDeviceResponse(u.Id,u.DeviceId, u.Hostname!, u.LocalIp!,
            u.CaptureSinceUtc, u.LastCaptureUtc, u.AppVersion, u.Status))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<EdgeDeviceResponse>> ListAsync(string? search, int pageNumber, int pageSize, CancellationToken ct)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

        var query = db.EdgeDevices.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search;
            query = query.Where(u =>
                (u.DeviceId ?? "").Contains(s) ||
                (u.LocalIp ?? "").Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CaptureSinceUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
                      .Select(u => new EdgeDeviceResponse(u.Id, u.DeviceId, u.Hostname!, u.LocalIp!,
            u.CaptureSinceUtc, u.LastCaptureUtc, u.AppVersion, u.Status))
            .ToListAsync(ct);

        return new PagedResult<EdgeDeviceResponse>(items, total, pageNumber, pageSize);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.EdgeDevices.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.EdgeDevices.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
