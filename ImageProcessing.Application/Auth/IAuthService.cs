using System.Security.Claims;

namespace ImageProcessing.Application.Auth;
public interface IAuthService
{
    Task<TokenResponse> LoginAsync(string email, string password, string ip, CancellationToken ct);
    Task<TokenResponse> RefreshAsync(string refreshToken, string ip, CancellationToken ct);
    Task RevokeAsync(string refreshToken, string ip, CancellationToken ct);
}
