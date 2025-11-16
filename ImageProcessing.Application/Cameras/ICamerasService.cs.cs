using ImageProcessing.Application.Common;

namespace ImageProcessing.Application.Cameras;

public interface ICamerasService
{
    Task<CameraResponse> CreateAsync(CreateCameraRequest req, CancellationToken ct);
    Task<CameraResponse> UpdateAsync(Guid Id,UpdateCameraRequest req, CancellationToken ct);
    
    Task<CameraResponse?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<PagedResult<CameraResponse>> ListAsync(
        string? search, int pageNumber, int pageSize, CancellationToken ct);

    Task<List<CameraResponse>> GetAll(
      string? search, CancellationToken ct);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
