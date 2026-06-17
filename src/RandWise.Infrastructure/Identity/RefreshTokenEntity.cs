namespace RandWise.Infrastructure.Identity;

public sealed class RefreshTokenEntity
{
    public string Id { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset ExpiresUtc { get; set; }

    public DateTimeOffset? RevokedUtc { get; set; }

    public string? ReplacedByTokenId { get; set; }
}
