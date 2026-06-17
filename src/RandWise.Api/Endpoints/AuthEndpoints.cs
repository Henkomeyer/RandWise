using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RandWise.Application.Auth;
using RandWise.Contracts.Auth;

namespace RandWise.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth")
            .WithTags("Authentication");

        auth.MapPost("/register", async (
                RegisterRequest request,
                IRandWiseAuthService authService,
                CancellationToken cancellationToken) =>
            await RunAuthOperationAsync(() => authService.RegisterAsync(request, cancellationToken)))
            .WithName("Register");

        auth.MapPost("/login", async (
                LoginRequest request,
                IRandWiseAuthService authService,
                CancellationToken cancellationToken) =>
            await RunAuthOperationAsync(() => authService.LoginAsync(request, cancellationToken)))
            .WithName("Login");

        auth.MapPost("/refresh", async (
                RefreshTokenRequest request,
                IRandWiseAuthService authService,
                CancellationToken cancellationToken) =>
            await RunAuthOperationAsync(() => authService.RefreshAsync(request, cancellationToken)))
            .WithName("RefreshToken");

        auth.MapPost("/logout", async (
                LogoutRequest request,
                IRandWiseAuthService authService,
                CancellationToken cancellationToken) =>
            {
                await authService.LogoutAsync(request, cancellationToken);
                return Results.NoContent();
            })
            .WithName("Logout");

        auth.MapGet("/me", async (
                ClaimsPrincipal user,
                IRandWiseAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var response = await authService.GetMeAsync(user, cancellationToken);
                return response is null ? Results.Unauthorized() : Results.Ok(response);
            })
            .RequireAuthorization()
            .WithName("GetCurrentUser");

        auth.MapPost("/request-password-reset", (RequestPasswordResetRequest request) => PersistenceRequired())
            .WithName("RequestPasswordReset");

        auth.MapPost("/reset-password", (ResetPasswordRequest request) => PersistenceRequired())
            .WithName("ResetPassword");

        return api;
    }

    private static async Task<IResult> RunAuthOperationAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return Results.Ok(await operation());
        }
        catch (AuthFailureException exception)
        {
            return ToProblem(exception);
        }
    }

    private static IResult PersistenceRequired()
    {
        return Results.Problem(
            title: "Authentication flow is not implemented.",
            detail: "This auth endpoint is reserved in the /api/v1 contract but is outside the current authentication persistence scope.",
            statusCode: StatusCodes.Status501NotImplemented);
    }

    private static IResult ToProblem(AuthFailureException exception)
    {
        if (exception.Failure == AuthFailure.InvalidCredentials)
        {
            return Results.Problem(
                title: "Invalid credentials.",
                detail: "Email or password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (exception.Failure == AuthFailure.InvalidRefreshToken)
        {
            return Results.Problem(
                title: "Invalid refresh token.",
                detail: "Refresh token is invalid or expired.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (exception.Failure == AuthFailure.DuplicateEmail)
        {
            return Results.Problem(
                title: "Email already registered.",
                detail: "Use a different email address or login.",
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Problem(
            title: "Authentication request is invalid.",
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
}
