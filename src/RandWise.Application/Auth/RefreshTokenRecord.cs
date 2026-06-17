namespace RandWise.Application.Auth;

public sealed record RefreshTokenRecord(
    string Id,
    string UserId,
    string TokenHash,
    DateTimeOffset CreatedUtc,
    DateTimeOffset ExpiresUtc,
    DateTimeOffset? RevokedUtc,
    string? ReplacedByTokenId);
