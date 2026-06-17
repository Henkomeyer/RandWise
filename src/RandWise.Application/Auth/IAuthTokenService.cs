namespace RandWise.Application.Auth;

public interface IAuthTokenService
{
    Task<AuthTokenSet> IssueTokenSetAsync(AuthenticatedUserContext user, CancellationToken cancellationToken);

    Task<AuthTokenSet> RotateRefreshTokenAsync(
        string presentedRefreshToken,
        AuthenticatedSessionContext session,
        CancellationToken cancellationToken);
}
