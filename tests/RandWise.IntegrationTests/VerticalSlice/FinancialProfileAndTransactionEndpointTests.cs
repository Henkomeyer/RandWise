using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RandWise.Contracts.Auth;
using RandWise.Contracts.Common;
using RandWise.Contracts.FinancialProfile;
using RandWise.Contracts.Transactions;
using RandWise.Infrastructure.Persistence;

namespace RandWise.IntegrationTests.VerticalSlice;

public sealed class FinancialProfileAndTransactionEndpointTests
{
    [Fact]
    public async Task AuthenticatedUser_CanOnboardAndManageTransactionLifecycle()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await RegisterAsync(client, "slice@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var profileRequest = new FinancialProfileRequest(
            2500000,
            25,
            "paydayToPayday",
            150000,
            50000,
            100000,
            "confirm",
            "Monday");

        using var putProfileResponse = await client.PutAsJsonAsync("/api/v1/financial-profile", profileRequest);
        putProfileResponse.EnsureSuccessStatusCode();
        var profile = await ReadAsync<FinancialProfileResponse>(putProfileResponse);
        Assert.Equal(2500000, profile.DefaultMonthlyIncomeCents);

        using var getProfileResponse = await client.GetAsync("/api/v1/financial-profile");
        getProfileResponse.EnsureSuccessStatusCode();

        var createRequest = new CreateTransactionRequest(
            25000,
            "expense",
            null,
            "Petrol",
            "Shell",
            new DateOnly(2026, 06, 14),
            "web");

        using var createResponse = await client.PostAsJsonAsync("/api/v1/transactions", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await ReadAsync<TransactionResponse>(createResponse);
        Assert.Equal(25000, created.AmountInCents);
        Assert.Equal("expense", created.TransactionType);
        Assert.Equal("confirmed", created.Status);

        using var listResponse = await client.GetAsync("/api/v1/transactions?page=1&pageSize=10");
        listResponse.EnsureSuccessStatusCode();
        var page = await ReadAsync<PagedResponse<TransactionResponse>>(listResponse);
        Assert.Single(page.Items);

        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/transactions/{created.Id}",
            new UpdateTransactionRequest(
                30000,
                "expense",
                created.CategoryId,
                "Fuel",
                "Shell",
                new DateOnly(2026, 06, 15),
                "Filled tank"));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await ReadAsync<TransactionResponse>(updateResponse);
        Assert.Equal(30000, updated.AmountInCents);
        Assert.Equal("Fuel", updated.Description);

        using var deleteResponse = await client.DeleteAsync($"/api/v1/transactions/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var restoreResponse = await client.PostAsync($"/api/v1/transactions/{created.Id}/restore", null);
        restoreResponse.EnsureSuccessStatusCode();
        var restored = await ReadAsync<TransactionResponse>(restoreResponse);
        Assert.Null(restored.DeletedUtc);
        Assert.Equal("confirmed", restored.Status);
    }

    [Fact]
    public async Task Transactions_AreFilteredByAuthenticatedUser()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var firstClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var secondClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var firstTokens = await RegisterAsync(firstClient, "first@example.com");
        var secondTokens = await RegisterAsync(secondClient, "second@example.com");
        firstClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstTokens.AccessToken);
        secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secondTokens.AccessToken);

        using var createResponse = await firstClient.PostAsJsonAsync(
            "/api/v1/transactions",
            new CreateTransactionRequest(12000, "expense", null, "Groceries", null, new DateOnly(2026, 06, 14), "web"));
        createResponse.EnsureSuccessStatusCode();

        using var secondListResponse = await secondClient.GetAsync("/api/v1/transactions");
        secondListResponse.EnsureSuccessStatusCode();
        var secondPage = await ReadAsync<PagedResponse<TransactionResponse>>(secondListResponse);
        Assert.Empty(secondPage.Items);
    }

    private static async Task<AuthTokenResponse> RegisterAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "CorrectHorseBatteryStaple1", "Test User"));

        response.EnsureSuccessStatusCode();
        return await ReadAsync<AuthTokenResponse>(response);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        where T : notnull
    {
        var body = await response.Content.ReadFromJsonAsync<T>();
        return Assert.IsType<T>(body);
    }

    private sealed class TestApplication : IAsyncDisposable
    {
        private readonly string databasePath;
        private readonly WebApplicationFactory<Program> factory;

        private TestApplication(string databasePath, WebApplicationFactory<Program> factory)
        {
            this.databasePath = databasePath;
            this.factory = factory;
        }

        public static async Task<TestApplication> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"randwise-slice-{Guid.NewGuid():N}.db");
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

            var application = new TestApplication(databasePath, factory);
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
