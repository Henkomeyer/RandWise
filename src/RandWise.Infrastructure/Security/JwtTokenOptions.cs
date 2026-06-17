namespace RandWise.Infrastructure.Security;

public sealed class JwtTokenOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "RandWise";

    public string Audience { get; init; } = "RandWise";

    public string SigningKey { get; init; } = string.Empty;

    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenDays { get; init; } = 30;
}
