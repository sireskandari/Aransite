using ImageProcessing.Application.Common;

namespace ImageProcessing.Application.LogEvents;

public interface ILogEventService
{
    Task<LogEventResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> DeleteAllAsync(CancellationToken ct);
    Task<PagedResult<LogEventResponse>> ListAsync(
        string? search, int pageNumber, int pageSize, CancellationToken ct);

}
