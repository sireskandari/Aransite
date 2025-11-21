using ImageProcessing.Application.Common;

namespace ImageProcessing.Application.EdgeDevices;

public interface IEdgeDevicesService
{
    Task<EdgeDeviceResponse> CreateAsync(CreateEdgeDeviceRequest req, CancellationToken ct);
    Task<EdgeDeviceResponse?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<PagedResult<EdgeDeviceResponse>> ListAsync(
        string? search, int pageNumber, int pageSize, CancellationToken ct);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
