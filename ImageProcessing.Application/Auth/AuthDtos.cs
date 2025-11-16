namespace ImageProcessing.Application.Auth;

public sealed record LoginRequest(string Email, string Password);
public sealed record TokenResponse(string AccessToken, DateTime AccessTokenExpiresUtc, string RefreshToken, DateTime RefreshTokenExpiresUtc);
public sealed record RefreshRequest(string RefreshToken);
