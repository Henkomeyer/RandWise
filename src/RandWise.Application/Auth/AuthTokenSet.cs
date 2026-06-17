namespace RandWise.Application.Auth;

public sealed record AuthTokenSet(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresUtc);
