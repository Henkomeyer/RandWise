using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RandWise.Contracts.Auth;
using RandWise.Contracts.FinancialProfile;
using RandWise.Infrastructure.Persistence;

namespace RandWise.IntegrationTests.FinancialProfile;

public sealed class FinancialProfileEndpointIntegrationTests
{
    [Fact]
    public async Task PutCreatesThenUpdatesCurrentUsersFinancialProfile()
    {
        await using var factory = await FinancialProfileTestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await AuthenticateAsync(client, "profile-owner@example.com");

        using var initialGetResponse = await client.GetAsync("/api/v1/financial-profile");
        Assert.Equal(HttpStatusCode.NotFound, initialGetResponse.StatusCode);

        var createRequest = new FinancialProfileRequest(
            3250000,
            25,
            "paydayToPayday",
            125000,
            50000,
            75000,
            "coach",
            "monday");

        using var createResponse = await client.PutAsJsonAsync("/api/v1/financial-profile", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await ReadAsync<FinancialProfileResponse>(createResponse);

        Assert.False(string.IsNullOrWhiteSpace(created.Id));
        Assert.Equal(3250000, created.DefaultMonthlyIncomeCents);
        Assert.Equal(25, created.PaydayDay);
        Assert.Equal("paydayToPayday", created.BudgetCycleType);
        Assert.Equal(125000, created.StartingBalanceCents);
        Assert.Equal(50000, created.SafetyBufferCents);
        Assert.Equal(75000, created.SavingsCommitmentCents);
        Assert.Equal("coach", created.NotificationMode);
        Assert.Equal("monday", created.FirstDayOfWeek);

        var updateRequest = createRequest with
        {
            DefaultMonthlyIncomeCents = 4000000,
            PaydayDay = null,
            BudgetCycleType = "calendarMonth",
            StartingBalanceCents = -25000,
            SafetyBufferCents = 100000,
            SavingsCommitmentCents = 150000,
            NotificationMode = "confirm",
            FirstDayOfWeek = "sunday"
        };

        using var updateResponse = await client.PutAsJsonAsync("/api/v1/financial-profile", updateRequest);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await ReadAsync<FinancialProfileResponse>(updateResponse);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(4000000, updated.DefaultMonthlyIncomeCents);
        Assert.Null(updated.PaydayDay);
        Assert.Equal("calendarMonth", updated.BudgetCycleType);
        Assert.Equal(-25000, updated.StartingBalanceCents);
        Assert.Equal(100000, updated.SafetyBufferCents);
        Assert.Equal(150000, updated.SavingsCommitmentCents);
        Assert.Equal("confirm", updated.NotificationMode);
        Assert.Equal("sunday", updated.FirstDayOfWeek);
        Assert.True(updated.UpdatedUtc >= created.UpdatedUtc);

        using var getResponse = await client.GetAsync("/api/v1/financial-profile");
        getResponse.EnsureSuccessStatusCode();
        var retrieved = await ReadAsync<FinancialProfileResponse>(getResponse);

        Assert.Equal(updated, retrieved);

        await factory.UseContextAsync(async context =>
        {
            var profile = await context.FinancialProfiles.SingleAsync();
            var appUser = await context.AppUsers.SingleAsync();

            Assert.Equal(appUser.Id, profile.UserId);
        });
    }

    [Fact]
    public async Task GetAndPutAreIsolatedByAuthenticatedUser()
    {
        await using var factory = await FinancialProfileTestApplication.CreateAsync();
        using var firstClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var secondClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await AuthenticateAsync(firstClient, "first-profile@example.com");
        await AuthenticateAsync(secondClient, "second-profile@example.com");

        var firstRequest = new FinancialProfileRequest(2000000, 20, "paydayToPayday", 0, 25000, 50000, "silent", "monday");
        using var firstPutResponse = await firstClient.PutAsJsonAsync("/api/v1/financial-profile", firstRequest);
        firstPutResponse.EnsureSuccessStatusCode();
        var firstProfile = await ReadAsync<FinancialProfileResponse>(firstPutResponse);

        using var secondGetBeforeCreateResponse = await secondClient.GetAsync("/api/v1/financial-profile");
        Assert.Equal(HttpStatusCode.NotFound, secondGetBeforeCreateResponse.StatusCode);

        var secondRequest = new FinancialProfileRequest(900000, 1, "calendarMonth", 10000, 15000, 0, "confirm", "saturday");
        using var secondPutResponse = await secondClient.PutAsJsonAsync("/api/v1/financial-profile", secondRequest);
        secondPutResponse.EnsureSuccessStatusCode();
        var secondProfile = await ReadAsync<FinancialProfileResponse>(secondPutResponse);

        Assert.NotEqual(firstProfile.Id, secondProfile.Id);
        Assert.Equal(2000000, firstProfile.DefaultMonthlyIncomeCents);
        Assert.Equal(900000, secondProfile.DefaultMonthlyIncomeCents);

        using var firstGetResponse = await firstClient.GetAsync("/api/v1/financial-profile");
        firstGetResponse.EnsureSuccessStatusCode();
        var firstRetrieved = await ReadAsync<FinancialProfileResponse>(firstGetResponse);

        Assert.Equal(firstProfile.Id, firstRetrieved.Id);
        Assert.Equal(2000000, firstRetrieved.DefaultMonthlyIncomeCents);

        await factory.UseContextAsync(async context =>
        {
            var profiles = await context.FinancialProfiles.AsNoTracking().ToArrayAsync();
            var users = await context.AppUsers.AsNoTracking().ToArrayAsync();

            Assert.Equal(2, profiles.Length);
            Assert.All(profiles, profile => Assert.Contains(users, user => user.Id == profile.UserId));
            Assert.Equal(2, profiles.Select(profile => profile.UserId).Distinct(StringComparer.Ordinal).Count());
        });
    }

    [Fact]
    public async Task PutRejectsInvalidProfileValues()
    {
        await using var factory = await FinancialProfileTestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await AuthenticateAsync(client, "invalid-profile@example.com");

        var request = new FinancialProfileRequest(-1, 32, "weekly", 0, -1, -1, "loud", "funday");

        using var response = await client.PutAsJsonAsync("/api/v1/financial-profile", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task AuthenticateAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "CorrectHorseBatteryStaple1", email[..email.IndexOf('@', StringComparison.Ordinal)]));
        response.EnsureSuccessStatusCode();
        var tokens = await ReadAsync<AuthTokenResponse>(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        where T : notnull
    {
        var body = await response.Content.ReadFromJsonAsync<T>();
        return Assert.IsType<T>(body);
    }

    private sealed class FinancialProfileTestApplication : IAsyncDisposable
    {
        private readonly string databasePath;
        private readonly WebApplicationFactory<Program> factory;

        private FinancialProfileTestApplication(string databasePath, WebApplicationFactory<Program> factory)
        {
            this.databasePath = databasePath;
            this.factory = factory;
        }

        public static async Task<FinancialProfileTestApplication> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"randwise-financial-profile-{Guid.NewGuid():N}.db");
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

            var application = new FinancialProfileTestApplication(databasePath, factory);
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
