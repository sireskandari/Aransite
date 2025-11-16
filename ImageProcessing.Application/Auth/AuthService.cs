using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Domain.Entities.Auth;
using ImageProcessing.Domain.Entities.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCryptNet = BCrypt.Net.BCrypt;

namespace ImageProcessing.Application.Auth;

public sealed class AuthService(IAppDbContext db, IConfiguration cfg) : IAuthService
{
    private readonly IConfiguration _cfg = cfg;

    public async Task<TokenResponse> LoginAsync(string email, string password, string ip, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct)
                   ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCryptNet.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var access = CreateAccessToken(user);
        var refresh = await CreateAndStoreRefreshTokenAsync(user, ip, ct);

        return new TokenResponse(access.Token, access.ExpiresUtc, refresh.Token, refresh.ExpiresUtc);
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken, string ip, CancellationToken ct)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken, ct)
                    ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!token.IsActive)
            throw new UnauthorizedAccessException("Inactive refresh token.");

        // rotate: create new, revoke old
        var user = await db.Users.FirstAsync(u => u.Id == token.UserId, ct);
        var newAccess = CreateAccessToken(user);
        var newRefresh = await CreateAndStoreRefreshTokenAsync(user, ip, ct);

        token.RevokedUtc = DateTime.UtcNow;
        token.RevokedByIp = ip;
        token.ReplacedByToken = newRefresh.Token;

        await db.SaveChangesAsync(ct);

        return new TokenResponse(newAccess.Token, newAccess.ExpiresUtc, newRefresh.Token, newRefresh.ExpiresUtc);
    }

    public async Task RevokeAsync(string refreshToken, string ip, CancellationToken ct)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken, ct)
                    ?? throw new KeyNotFoundException("Refresh token not found.");

        if (!token.IsActive) return; // already inactive
        token.RevokedUtc = DateTime.UtcNow;
        token.RevokedByIp = ip;

        await db.SaveChangesAsync(ct);
    }

    // helpers
    private (string Token, DateTime ExpiresUtc) CreateAccessToken(User user)
    {
        var key = _cfg["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");
        var issuer = _cfg["Jwt:Issuer"];
        var audience = _cfg["Jwt:Audience"];
        var minutes = int.TryParse(_cfg["Jwt:AccessMinutes"], out var m) ? m : 30;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role),
            new("name", user.Name ?? "")
        };

        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(minutes);

        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwt, expires);
    }

    private async Task<RefreshToken> CreateAndStoreRefreshTokenAsync(User user, string ip, CancellationToken ct)
    {
        var days = int.TryParse(_cfg["Jwt:RefreshDays"], out var d) ? d : 14;
        var rt = new RefreshToken
        {
            UserId = user.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            CreatedUtc = DateTime.UtcNow,
            CreatedByIp = ip,
            ExpiresUtc = DateTime.UtcNow.AddDays(days),
        };
        db.RefreshTokens.Add(rt);
        await db.SaveChangesAsync(ct);
        return rt;
    }
}
