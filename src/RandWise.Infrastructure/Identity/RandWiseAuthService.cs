using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RandWise.Application.Auth;
using RandWise.Application.Security;
using RandWise.Contracts.Auth;
using RandWise.Domain.Entities;
using RandWise.Infrastructure.Persistence;

namespace RandWise.Infrastructure.Identity;

public sealed class RandWiseAuthService : IRandWiseAuthService
{
    private readonly UserManager<RandWiseIdentityUser> userManager;
    private readonly RandWiseDbContext dbContext;
    private readonly IAuthTokenService tokenService;
    private readonly IRefreshTokenHasher refreshTokenHasher;
    private readonly IRefreshTokenStore refreshTokenStore;

    public RandWiseAuthService(
        UserManager<RandWiseIdentityUser> userManager,
        RandWiseDbContext dbContext,
        IAuthTokenService tokenService,
        IRefreshTokenHasher refreshTokenHasher,
        IRefreshTokenStore refreshTokenStore)
    {
        this.userManager = userManager;
        this.dbContext = dbContext;
        this.tokenService = tokenService;
        this.refreshTokenHasher = refreshTokenHasher;
        this.refreshTokenStore = refreshTokenStore;
    }

    public async Task<AuthTokenResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        ValidateRegister(request);

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            throw new AuthFailureException(AuthFailure.DuplicateEmail, "Email is already registered.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var identityUser = new RandWiseIdentityUser
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = request.Email,
            Email = request.Email
        };

        var identityResult = await userManager.CreateAsync(identityUser, request.Password);
        if (!identityResult.Succeeded)
        {
            throw ToValidationException(identityResult);
        }

        var now = DateTime.UtcNow;
        var appUser = AppUser.Create(Guid.NewGuid().ToString("N"), identityUser.Id, request.DisplayName, now);
        dbContext.AppUsers.Add(appUser);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToResponse(await tokenService.IssueTokenSetAsync(
            new AuthenticatedUserContext(appUser.Id, request.Email, appUser.DisplayName),
            cancellationToken));
    }

    public async Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AuthFailureException(AuthFailure.InvalidCredentials, "Email or password is incorrect.");
        }

        var identityUser = await userManager.FindByEmailAsync(request.Email);
        if (identityUser is null || !await userManager.CheckPasswordAsync(identityUser, request.Password))
        {
            throw new AuthFailureException(AuthFailure.InvalidCredentials, "Email or password is incorrect.");
        }

        var appUser = await FindActiveAppUserByIdentityIdAsync(identityUser.Id, cancellationToken);
        if (appUser is null)
        {
            throw new AuthFailureException(AuthFailure.InvalidCredentials, "Email or password is incorrect.");
        }

        return ToResponse(await tokenService.IssueTokenSetAsync(
            new AuthenticatedUserContext(appUser.Id, identityUser.Email ?? request.Email, appUser.DisplayName),
            cancellationToken));
    }

    public async Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new AuthFailureException(AuthFailure.InvalidRefreshToken, "Refresh token is invalid.");
        }

        var tokenHash = refreshTokenHasher.Hash(request.RefreshToken);
        var existingToken = await refreshTokenStore.FindActiveByHashAsync(tokenHash, cancellationToken);
        if (existingToken is null)
        {
            throw new AuthFailureException(AuthFailure.InvalidRefreshToken, "Refresh token is invalid.");
        }

        var appUser = await dbContext.AppUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == existingToken.UserId && user.DeletedUtc == null, cancellationToken);
        if (appUser is null)
        {
            throw new AuthFailureException(AuthFailure.InvalidRefreshToken, "Refresh token is invalid.");
        }

        var identityUser = await userManager.FindByIdAsync(appUser.IdentityUserId);
        if (identityUser?.Email is null)
        {
            throw new AuthFailureException(AuthFailure.InvalidRefreshToken, "Refresh token is invalid.");
        }

        return ToResponse(await tokenService.RotateRefreshTokenAsync(
            request.RefreshToken,
            new AuthenticatedSessionContext(
                appUser.Id,
                identityUser.Email,
                appUser.DisplayName,
                existingToken.Id,
                existingToken.TokenHash,
                existingToken.ExpiresUtc,
                existingToken.RevokedUtc),
            cancellationToken));
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var tokenHash = refreshTokenHasher.Hash(request.RefreshToken);
        var existingToken = await refreshTokenStore.FindActiveByHashAsync(tokenHash, cancellationToken);
        if (existingToken is null)
        {
            return;
        }

        await refreshTokenStore.RevokeAsync(existingToken.Id, DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task<MeResponse?> GetMeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var appUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("app_user_id")
            ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(appUserId))
        {
            throw new AuthFailureException(AuthFailure.InvalidPrincipal, "Authenticated principal has no user id.");
        }

        var appUser = await dbContext.AppUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == appUserId && user.DeletedUtc == null, cancellationToken);
        if (appUser is null)
        {
            return null;
        }

        var identityUser = await userManager.FindByIdAsync(appUser.IdentityUserId);
        if (identityUser?.Email is null)
        {
            return null;
        }

        return new MeResponse(
            identityUser.Email,
            appUser.DisplayName,
            appUser.PreferredCurrency,
            appUser.TimeZone,
            appUser.PreferredLanguage);
    }

    private async Task<AppUser?> FindActiveAppUserByIdentityIdAsync(
        string identityUserId,
        CancellationToken cancellationToken) =>
        await dbContext.AppUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.IdentityUserId == identityUserId && user.DeletedUtc == null,
                cancellationToken);

    private static void ValidateRegister(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new AuthFailureException(AuthFailure.ValidationFailed, "Email, password and display name are required.");
        }
    }

    private static AuthFailureException ToValidationException(IdentityResult identityResult)
    {
        var description = identityResult.Errors.FirstOrDefault()?.Description ?? "Registration failed.";
        return new AuthFailureException(AuthFailure.ValidationFailed, description);
    }

    private static AuthTokenResponse ToResponse(AuthTokenSet tokenSet) =>
        new(
            tokenSet.AccessToken,
            tokenSet.AccessTokenExpiresUtc,
            tokenSet.RefreshToken,
            tokenSet.RefreshTokenExpiresUtc);
}
