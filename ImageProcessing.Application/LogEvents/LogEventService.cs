using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace ImageProcessing.Application.LogEvents;

public sealed class LogEventService(IAppDbContext db) : ILogEventService
{

    public async Task<LogEventResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await db.LogEvents
            .Where(u => u.Id == id)
            .Select(u => new LogEventResponse(u.Id, u.Message!, u.MessageTemplate!, u.Level!, u.Exception!, u.Properties!, u.TimeStamp))
            .FirstOrDefaultAsync(ct);
    }
    public async Task<bool> DeleteAllAsync(CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE serilog_logs;", ct);
        return true;
    }


    public async Task<PagedResult<LogEventResponse>> ListAsync(string? search, int pageNumber, int pageSize, CancellationToken ct)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

        var query = db.LogEvents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                (u.Message ?? "").Contains(s) ||
                (u.MessageTemplate ?? "").Contains(s) ||
                (u.Properties ?? "").Contains(s) ||
                (u.Exception ?? "").Contains(s));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.TimeStamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new LogEventResponse(u.Id, u.Message!, u.MessageTemplate!, u.Level!, u.Exception!, u.Properties!, u.TimeStamp))
            .ToListAsync(ct);

        return new PagedResult<LogEventResponse>(items, total, pageNumber, pageSize);
    }

}
