using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RandWise.Contracts.Auth;
using RandWise.Infrastructure.Persistence;

namespace RandWise.IntegrationTests.Auth;

public sealed class AuthEndpointIntegrationTests
{
    [Fact]
    public async Task Register_ReturnsTokensCreatesLinkedAppUserAndAuthenticatedMe()
    {
        await using var factory = await AuthTestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var register = new RegisterRequest("register@example.com", "CorrectHorseBatteryStaple1", "Registered User");
        using var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", register);

        registerResponse.EnsureSuccessStatusCode();
        var tokens = await ReadAsync<AuthTokenResponse>(registerResponse);

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));

        await factory.UseContextAsync(async context =>
        {
            var appUser = await context.AppUsers.SingleAsync();
            var identityUser = await context.Users.SingleAsync();
            var refreshToken = await context.RefreshTokens.SingleAsync();

            Assert.Equal(identityUser.Id, appUser.IdentityUserId);
            Assert.Equal("Registered User", appUser.DisplayName);
            Assert.NotEqual(tokens.RefreshToken, refreshToken.TokenHash);
            Assert.StartsWith("sha256:", refreshToken.TokenHash, StringComparison.Ordinal);
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        using var meResponse = await client.GetAsync("/api/v1/auth/me?userId=client-supplied");

        meResponse.EnsureSuccessStatusCode();
        var me = await ReadAsync<MeResponse>(meResponse);
        Assert.Equal("register@example.com", me.Email);
        Assert.Equal("Registered User", me.DisplayName);
        Assert.Equal("ZAR", me.PreferredCurrency);
    }

    [Fact]
    public async Task LoginRefreshAndLogout_RotateAndRevokeRefreshTokens()
    {
        await using var factory = await AuthTestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var email = $"auth-{Guid.NewGuid():N}@example.com";
        var password = "CorrectHorseBatteryStaple1";
        using var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, password, "Auth User"));
        registerResponse.EnsureSuccessStatusCode();
        var registerTokens = await ReadAsync<AuthTokenResponse>(registerResponse);

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();
        var loginTokens = await ReadAsync<AuthTokenResponse>(loginResponse);

        Assert.NotEqual(registerTokens.RefreshToken, loginTokens.RefreshToken);

        using var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(loginTokens.RefreshToken));
        refreshResponse.EnsureSuccessStatusCode();
        var rotatedTokens = await ReadAsync<AuthTokenResponse>(refreshResponse);

        Assert.NotEqual(loginTokens.RefreshToken, rotatedTokens.RefreshToken);

        using var staleReuseResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(loginTokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, staleReuseResponse.StatusCode);

        await factory.UseContextAsync(async context =>
        {
            var refreshTokens = await context.RefreshTokens.ToArrayAsync();

            Assert.Equal(3, refreshTokens.Length);
            Assert.Contains(refreshTokens, token => token.RevokedUtc is not null && token.ReplacedByTokenId is not null);
            Assert.DoesNotContain(refreshTokens, token => token.TokenHash == loginTokens.RefreshToken);
            Assert.DoesNotContain(refreshTokens, token => token.TokenHash == rotatedTokens.RefreshToken);
        });

        using var logoutResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new LogoutRequest(rotatedTokens.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        using var loggedOutRefreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(rotatedTokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, loggedOutRefreshResponse.StatusCode);
    }

    [Fact]
    public async Task PasswordResetPlaceholders_RemainOutOfScope()
    {
        await using var factory = await AuthTestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var requestResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/request-password-reset",
            new RequestPasswordResetRequest("user@example.com"));
        using var resetResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/reset-password",
            new ResetPasswordRequest("user@example.com", "reset-token", "CorrectHorseBatteryStaple1"));

        Assert.Equal(HttpStatusCode.NotImplemented, requestResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotImplemented, resetResponse.StatusCode);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        where T : notnull
    {
        var body = await response.Content.ReadFromJsonAsync<T>();
        return Assert.IsType<T>(body);
    }

    private sealed class AuthTestApplication : IAsyncDisposable
    {
        private readonly string databasePath;
        private readonly WebApplicationFactory<Program> factory;

        private AuthTestApplication(string databasePath, WebApplicationFactory<Program> factory)
        {
            this.databasePath = databasePath;
            this.factory = factory;
        }

        public static async Task<AuthTestApplication> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"randwise-auth-{Guid.NewGuid():N}.db");
            var connectionString = $"Data Source={databasePath}";
            var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseSetting("Persistence:ConnectionString", connectionString);
                    builder.UseSetting("Jwt:Issuer", "RandWise.Tests");
                    builder.UseSetting("Jwt:Audience", "RandWise.Tests");
                    builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-chars");
                    builder.UseSetting("Jwt:AccessTokenMinutes", "15");
                    builder.UseSetting("Jwt:RefreshTokenDays", "30");
                });

            var application = new AuthTestApplication(databasePath, factory);
            await application.UseContextAsync(context => context.Database.MigrateAsync());

            return application;
        }

        public HttpClient CreateClient(WebApplicationFactoryClientOptions options) =>
            factory.CreateClient(options);

        public async Task UseContextAsync(Func<RandWiseDbContext, Task> action)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RandWiseDbContext>();
            await action(context);
        }

        public async ValueTask DisposeAsync()
        {
            await factory.DisposeAsync();
            TryDelete(databasePath);
            TryDelete($"{databasePath}-shm");
            TryDelete($"{databasePath}-wal");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
