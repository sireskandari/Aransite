using ImageProcessing.Application.Common;

namespace ImageProcessing.Application.Users;

public interface IUsersService
{
    Task<UserResponse> CreateAsync(CreateUserRequest req, CancellationToken ct);
    Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<PagedResult<UserResponse>> ListAsync(
        string? search, int pageNumber, int pageSize, CancellationToken ct);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
