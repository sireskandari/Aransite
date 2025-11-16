using ImageProcessing.Application.Common;

namespace ImageProcessing.Application.DetectTargets;

public interface IDetectTargetsService
{
    Task<DetectTargetResponse> CreateAsync(CreateDetectTargetRequest req, CancellationToken ct);
    Task<DetectTargetResponse> UpdateAsync(Guid Id, UpdateDetectTargetRequest req, CancellationToken ct);
    Task<DetectTargetResponse?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<PagedResult<DetectTargetResponse>> ListAsync(
        string? search, int pageNumber, int pageSize, CancellationToken ct);
    Task<List<DetectTargetResponse>> GetAll(string? search, CancellationToken ct);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
