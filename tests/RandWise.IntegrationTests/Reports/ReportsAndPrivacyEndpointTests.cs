using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RandWise.Contracts.Auth;
using RandWise.Contracts.Categories;
using RandWise.Contracts.Profile;
using RandWise.Contracts.Reports;
using RandWise.Contracts.Transactions;
using RandWise.Infrastructure.Persistence;

namespace RandWise.IntegrationTests.Reports;

public sealed class ReportsAndPrivacyEndpointTests
{
    [Fact]
    public async Task ReportsCsvAndProfileExport_AreScopedToAuthenticatedUser()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var firstClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var secondClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var firstTokens = await RegisterAsync(firstClient, "reports-one@example.com");
        var secondTokens = await RegisterAsync(secondClient, "reports-two@example.com");
        firstClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstTokens.AccessToken);
        secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secondTokens.AccessToken);

        var groceries = await CreateCategoryAsync(firstClient, "Groceries", "expense");
        var salary = await CreateCategoryAsync(firstClient, "Salary", "income");
        var otherGroceries = await CreateCategoryAsync(secondClient, "Other Groceries", "expense");

        await CreateTransactionAsync(firstClient, groceries.Id, 12500, "expense", "Checkers", "web", new DateOnly(2026, 6, 16));
        await CreateTransactionAsync(firstClient, salary.Id, 500000, "income", "June salary", "web", new DateOnly(2026, 6, 15));
        await CreateTransactionAsync(secondClient, otherGroceries.Id, 99900, "expense", "Other spend", "web", new DateOnly(2026, 6, 16));

        using var weeklyResponse = await firstClient.GetAsync("/api/v1/reports/weekly?weekStart=2026-06-15");
        weeklyResponse.EnsureSuccessStatusCode();
        var weekly = await ReadAsync<WeeklyFinancialStoryResponse>(weeklyResponse);
        Assert.Equal(500000, weekly.IncomeInCents);
        Assert.Equal(12500, weekly.ExpenseInCents);
        var topCategory = Assert.Single(weekly.TopCategories);
        Assert.Equal("Groceries", topCategory.CategoryName);

        using var monthlyResponse = await firstClient.GetAsync("/api/v1/reports/monthly?year=2026&month=6");
        monthlyResponse.EnsureSuccessStatusCode();
        var monthly = await ReadAsync<MonthlyMoneyWrapResponse>(monthlyResponse);
        Assert.Equal(2026, monthly.Year);
        Assert.Equal(6, monthly.Month);
        Assert.Equal(12500, monthly.ExpenseInCents);

        using var csvResponse = await firstClient.GetAsync("/api/v1/reports/export/csv");
        csvResponse.EnsureSuccessStatusCode();
        var csv = await csvResponse.Content.ReadAsStringAsync();
        Assert.Contains("Checkers", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("Other spend", csv, StringComparison.Ordinal);

        using var exportResponse = await firstClient.GetAsync("/api/v1/profile/export");
        exportResponse.EnsureSuccessStatusCode();
        var export = await ReadAsync<ProfileExportResponse>(exportResponse);
        Assert.Equal("WhatsApp User", export.User.DisplayName);
        Assert.Equal(2, export.Transactions.Count);

        await factory.UseContextAsync(async context =>
        {
            var auditEvents = await context.AuditLogs
                .Select(log => log.EventType)
                .ToListAsync();
            Assert.Contains("report.weekly.viewed", auditEvents);
            Assert.Contains("report.monthly.viewed", auditEvents);
            Assert.Contains("report.csv.exported", auditEvents);
            Assert.Contains("privacy.data_exported", auditEvents);
        });
    }

    [Fact]
    public async Task DeleteProfile_RemovesOwnedFinancialDataAndBlocksFutureLogin()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await RegisterAsync(client, "delete-me@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var category = await CreateCategoryAsync(client, "Groceries", "expense");
        await CreateTransactionAsync(client, category.Id, 25000, "expense", "Checkers", "web", new DateOnly(2026, 6, 17));

        using var deleteResponse = await client.DeleteAsync("/api/v1/profile");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("delete-me@example.com", "CorrectHorseBatteryStaple1"));
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);

        await factory.UseContextAsync(async context =>
        {
            var user = await context.AppUsers.SingleAsync();
            Assert.NotNull(user.DeletedUtc);
            Assert.StartsWith("Deleted user", user.DisplayName, StringComparison.Ordinal);
            Assert.Empty(await context.Transactions.ToListAsync());
            Assert.Empty(await context.BudgetCategories.Where(category => !category.IsSystem).ToListAsync());
            Assert.Contains(await context.AuditLogs.ToListAsync(), log => log.EventType == "privacy.account_deleted");
        });
    }

    private static async Task<AuthTokenResponse> RegisterAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "CorrectHorseBatteryStaple1", "WhatsApp User"));

        response.EnsureSuccessStatusCode();
        return await ReadAsync<AuthTokenResponse>(response);
    }

    private static async Task<CategoryResponse> CreateCategoryAsync(HttpClient client, string name, string type)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/categories",
            new CategoryRequest(name, type, null, 10));
        response.EnsureSuccessStatusCode();
        return await ReadAsync<CategoryResponse>(response);
    }

    private static async Task<TransactionResponse> CreateTransactionAsync(
        HttpClient client,
        string categoryId,
        long amountInCents,
        string transactionType,
        string description,
        string source,
        DateOnly transactionDate)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new CreateTransactionRequest(amountInCents, transactionType, categoryId, description, null, transactionDate, source));
        response.EnsureSuccessStatusCode();
        return await ReadAsync<TransactionResponse>(response);
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
            var databasePath = Path.Combine(Path.GetTempPath(), $"randwise-reports-{Guid.NewGuid():N}.db");
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
                    builder.UseSetting("SensitiveData:Key", "integration-sensitive-data-key-at-least-32-chars");
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
