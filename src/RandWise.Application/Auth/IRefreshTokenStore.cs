namespace RandWise.Application.Auth;

public interface IRefreshTokenStore
{
    Task<RefreshTokenRecord?> FindActiveByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task StoreAsync(RefreshTokenRecord refreshToken, CancellationToken cancellationToken);

    Task RotateAsync(
        string existingRefreshTokenId,
        RefreshTokenRecord replacementRefreshToken,
        DateTimeOffset revokedUtc,
        CancellationToken cancellationToken);

    Task RevokeAsync(string refreshTokenId, DateTimeOffset revokedUtc, CancellationToken cancellationToken);
}
