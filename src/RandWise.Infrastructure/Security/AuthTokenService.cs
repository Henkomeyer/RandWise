using Microsoft.Extensions.Options;
using RandWise.Application.Auth;
using RandWise.Application.Security;

namespace RandWise.Infrastructure.Security;

public sealed class AuthTokenService : IAuthTokenService
{
    private readonly IAccessTokenIssuer accessTokenIssuer;
    private readonly IRefreshTokenGenerator refreshTokenGenerator;
    private readonly IRefreshTokenHasher refreshTokenHasher;
    private readonly IRefreshTokenStore refreshTokenStore;
    private readonly JwtTokenOptions options;

    public AuthTokenService(
        IAccessTokenIssuer accessTokenIssuer,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IRefreshTokenStore refreshTokenStore,
        IOptions<JwtTokenOptions> options)
    {
        this.accessTokenIssuer = accessTokenIssuer;
        this.refreshTokenGenerator = refreshTokenGenerator;
        this.refreshTokenHasher = refreshTokenHasher;
        this.refreshTokenStore = refreshTokenStore;
        this.options = options.Value;
    }

    public async Task<AuthTokenSet> IssueTokenSetAsync(AuthenticatedUserContext user, CancellationToken cancellationToken)
    {
        var issuedUtc = DateTimeOffset.UtcNow;
        var accessTokenExpiresUtc = issuedUtc.AddMinutes(options.AccessTokenMinutes);
        var refreshTokenExpiresUtc = issuedUtc.AddDays(options.RefreshTokenDays);
        var rawRefreshToken = refreshTokenGenerator.Generate();

        var accessToken = accessTokenIssuer.IssueAccessToken(new AccessTokenDescriptor(
            user.UserId,
            user.Email,
            user.DisplayName,
            issuedUtc,
            accessTokenExpiresUtc));

        await refreshTokenStore.StoreAsync(
            new RefreshTokenRecord(
                Guid.NewGuid().ToString("N"),
                user.UserId,
                refreshTokenHasher.Hash(rawRefreshToken),
                issuedUtc,
                refreshTokenExpiresUtc,
                null,
                null),
            cancellationToken);

        return new AuthTokenSet(accessToken, accessTokenExpiresUtc, rawRefreshToken, refreshTokenExpiresUtc);
    }

    public async Task<AuthTokenSet> RotateRefreshTokenAsync(
        string presentedRefreshToken,
        AuthenticatedSessionContext session,
        CancellationToken cancellationToken)
    {
        var existingHash = refreshTokenHasher.Hash(presentedRefreshToken);
        var existing = await refreshTokenStore.FindActiveByHashAsync(existingHash, cancellationToken);
        if (existing is null || existing.UserId != session.UserId)
        {
            throw new AuthFailureException(AuthFailure.InvalidRefreshToken, "Refresh token is invalid.");
        }

        var issuedUtc = DateTimeOffset.UtcNow;
        var accessTokenExpiresUtc = issuedUtc.AddMinutes(options.AccessTokenMinutes);
        var refreshTokenExpiresUtc = issuedUtc.AddDays(options.RefreshTokenDays);
        var rawRefreshToken = refreshTokenGenerator.Generate();
        var replacement = new RefreshTokenRecord(
            Guid.NewGuid().ToString("N"),
            session.UserId,
            refreshTokenHasher.Hash(rawRefreshToken),
            issuedUtc,
            refreshTokenExpiresUtc,
            null,
            null);

        var accessToken = accessTokenIssuer.IssueAccessToken(new AccessTokenDescriptor(
            session.UserId,
            session.Email,
            session.DisplayName,
            issuedUtc,
            accessTokenExpiresUtc));

        await refreshTokenStore.RotateAsync(existing.Id, replacement, issuedUtc, cancellationToken);

        return new AuthTokenSet(accessToken, accessTokenExpiresUtc, rawRefreshToken, refreshTokenExpiresUtc);
    }
}
