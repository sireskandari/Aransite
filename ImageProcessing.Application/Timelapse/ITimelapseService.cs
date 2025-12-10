using ImageProcessing.Application.Common;
using ImageProcessing.Application.Timelapses;

namespace ImageProcessing.Application.Timelapse;

public interface ITimelapseService
{
    Task<TimelapseResponse> CreateAsync(CreateTimelapseRequest req, CancellationToken ct);
    Task<TimelapseResponse> UpdateAsync(Guid Id,UpdateTimelapseRequest req, CancellationToken ct);
    
    Task<TimelapseResponse?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<PagedResult<TimelapseResponse>> ListAsync(
        string? search, int pageNumber, int pageSize, CancellationToken ct);

    Task<List<TimelapseResponse>> GetAll(
      string? search, CancellationToken ct);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
