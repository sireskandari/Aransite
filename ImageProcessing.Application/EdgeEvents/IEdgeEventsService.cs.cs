using ImageProcessing.Application.Cameras;
using ImageProcessing.Application.Common;

namespace ImageProcessing.Application.EdgeEvents;

public interface IEdgeEventsService
{
    Task<EdgeEventsResponse> CreateAsync(CreateEdgeEventsRequest req, CancellationToken ct);
    Task<EdgeEventsResponse?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<PagedResult<EdgeEventsResponse>> ListAsync(
        string? search, int pageNumber, int pageSize, CancellationToken ct);

    Task<List<EdgeEventsResponse>> GetAll(
<<<<<<< HEAD
      string? search, DateTime? fromUtc,DateTime? toUtc, CancellationToken ct);
=======
      string? search, CancellationToken ct);
>>>>>>> b186aa7 (v4)

    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
