namespace RandWise.Contracts.Auth;

public sealed record AuthTokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresUtc,
    string TokenType = "Bearer");
