using System.Security.Claims;
using RandWise.Contracts.Auth;

namespace RandWise.Application.Auth;

public interface IRandWiseAuthService
{
    Task<AuthTokenResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);

    Task<MeResponse?> GetMeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
