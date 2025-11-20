using ImageProcessing.Application.Cameras;
using ImageProcessing.Application.Common;

namespace ImageProcessing.Application.EdgeEvents;

public interface IEdgeEventsService
{
    Task<EdgeEventsResponse> CreateAsync(CreateEdgeEventsRequest req, CancellationToken ct);
    Task<EdgeEventsResponse?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<PagedResult<EdgeEventsResponse>> ListAsync(string? search, string? cameraId, int pageNumber, int pageSize, CancellationToken ct);

    Task<List<EdgeEventsResponse>> GetAll(string? search, string? cameraId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
