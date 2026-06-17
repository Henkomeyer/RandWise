namespace RandWise.Application.Auth;

public sealed record AuthenticatedSessionContext(
    string UserId,
    string Email,
    string DisplayName,
    string RefreshTokenId,
    string RefreshTokenHash,
    DateTimeOffset ExpiresUtc,
    DateTimeOffset? RevokedUtc);
