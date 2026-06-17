using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RandWise.Contracts.Auth;
using RandWise.Contracts.Budgeting;
using RandWise.Contracts.Categories;
using RandWise.Contracts.Dashboard;
using RandWise.Contracts.FinancialProfile;
using RandWise.Contracts.RecurringTransactions;
using RandWise.Contracts.Transactions;
using RandWise.Infrastructure.Persistence;

namespace RandWise.IntegrationTests.Budgeting;

public sealed class BudgetingEndpointIntegrationTests
{
    [Fact]
    public async Task AuthenticatedUser_CanManageBudgetingSliceAndReadSafeToSpend()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await RegisterAsync(client, "budget-owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var profileResponse = await client.PutAsJsonAsync(
            "/api/v1/financial-profile",
            new FinancialProfileRequest(1000000, 25, "paydayToPayday", 300000, 50000, 100000, "confirm", "Monday"));
        profileResponse.EnsureSuccessStatusCode();

        using var categoryResponse = await client.PostAsJsonAsync(
            "/api/v1/categories",
            new CategoryRequest("Groceries", "expense", "basket", 10));
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var category = await ReadAsync<CategoryResponse>(categoryResponse);
        Assert.Equal("groceries", category.Slug);

        using var periodResponse = await client.PostAsJsonAsync(
            "/api/v1/budget-periods",
            new BudgetPeriodRequest(today.AddDays(-3), today.AddDays(11), 1000000, 300000));
        Assert.Equal(HttpStatusCode.Created, periodResponse.StatusCode);
        var period = await ReadAsync<BudgetPeriodResponse>(periodResponse);
        Assert.Equal("open", period.Status);

        using var currentResponse = await client.GetAsync("/api/v1/budget-periods/current");
        currentResponse.EnsureSuccessStatusCode();
        var currentPeriod = await ReadAsync<BudgetPeriodResponse>(currentResponse);
        Assert.Equal(period.Id, currentPeriod.Id);

        using var categoryBudgetResponse = await client.PostAsJsonAsync(
            $"/api/v1/budget-periods/{period.Id}/category-budgets",
            new CategoryBudgetRequest(category.Id, 400000, 0, 80));
        Assert.Equal(HttpStatusCode.Created, categoryBudgetResponse.StatusCode);
        var categoryBudget = await ReadAsync<CategoryBudgetResponse>(categoryBudgetResponse);
        Assert.Equal(category.Id, categoryBudget.CategoryId);

        using var expenseResponse = await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new CreateTransactionRequest(150000, "expense", category.Id, "Groceries", null, today, "web"));
        expenseResponse.EnsureSuccessStatusCode();

        using var incomeResponse = await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new CreateTransactionRequest(200000, "income", category.Id, "Freelance", null, today, "web"));
        incomeResponse.EnsureSuccessStatusCode();

        using var recurringResponse = await client.PostAsJsonAsync(
            "/api/v1/recurring-transactions",
            new RecurringTransactionRequest(
                category.Id,
                "Internet",
                "Fibre Co",
                75000,
                "expense",
                "monthly",
                today.Day,
                null,
                today.AddDays(3),
                null,
                true));
        Assert.Equal(HttpStatusCode.Created, recurringResponse.StatusCode);
        var recurring = await ReadAsync<RecurringTransactionResponse>(recurringResponse);
        Assert.True(recurring.IsActive);

        using var safeResponse = await client.GetAsync("/api/v1/dashboard/safe-to-spend");
        safeResponse.EnsureSuccessStatusCode();
        var safe = await ReadAsync<SafeToSpendResponse>(safeResponse);
        Assert.Equal(period.Id, safe.BudgetPeriodId);
        Assert.Equal(75000, safe.UpcomingCommitmentsInCents);
        Assert.Equal(250000, safe.RemainingCategoryBudgetInCents);
        Assert.Equal(250000, safe.AmountInCents);

        using var budgetListResponse = await client.GetAsync($"/api/v1/budget-periods/{period.Id}/category-budgets");
        budgetListResponse.EnsureSuccessStatusCode();
        var budgets = await ReadAsync<CategoryBudgetResponse[]>(budgetListResponse);
        Assert.Single(budgets);
        Assert.Equal(150000, budgets[0].SpentAmountCents);

        using var pauseResponse = await client.PostAsync($"/api/v1/recurring-transactions/{recurring.Id}/pause", null);
        pauseResponse.EnsureSuccessStatusCode();
        var paused = await ReadAsync<RecurringTransactionResponse>(pauseResponse);
        Assert.False(paused.IsActive);
    }

    [Fact]
    public async Task BudgetingResources_RejectCrossUserAccess()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var owner = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var intruder = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var ownerTokens = await RegisterAsync(owner, "budget-owner-2@example.com");
        var intruderTokens = await RegisterAsync(intruder, "budget-intruder@example.com");
        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerTokens.AccessToken);
        intruder.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", intruderTokens.AccessToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var categoryResponse = await owner.PostAsJsonAsync(
            "/api/v1/categories",
            new CategoryRequest("Transport", "expense", "car", 20));
        categoryResponse.EnsureSuccessStatusCode();
        var category = await ReadAsync<CategoryResponse>(categoryResponse);

        using var periodResponse = await owner.PostAsJsonAsync(
            "/api/v1/budget-periods",
            new BudgetPeriodRequest(today.AddDays(-1), today.AddDays(20), 1000000, 0));
        periodResponse.EnsureSuccessStatusCode();
        var period = await ReadAsync<BudgetPeriodResponse>(periodResponse);

        using var budgetResponse = await owner.PostAsJsonAsync(
            $"/api/v1/budget-periods/{period.Id}/category-budgets",
            new CategoryBudgetRequest(category.Id, 250000, 0, 75));
        budgetResponse.EnsureSuccessStatusCode();
        var budget = await ReadAsync<CategoryBudgetResponse>(budgetResponse);

        using var intruderListResponse = await intruder.GetAsync($"/api/v1/budget-periods/{period.Id}/category-budgets");
        Assert.Equal(HttpStatusCode.NotFound, intruderListResponse.StatusCode);

        using var intruderUpdateResponse = await intruder.PutAsJsonAsync(
            $"/api/v1/category-budgets/{budget.Id}",
            new CategoryBudgetRequest(category.Id, 1000, 0, 50));
        Assert.Equal(HttpStatusCode.NotFound, intruderUpdateResponse.StatusCode);

        using var intruderRecurringResponse = await intruder.PostAsJsonAsync(
            "/api/v1/recurring-transactions",
            new RecurringTransactionRequest(
                category.Id,
                "Stolen category",
                null,
                1000,
                "expense",
                "monthly",
                today.Day,
                null,
                today,
                null,
                true));
        Assert.Equal(HttpStatusCode.BadRequest, intruderRecurringResponse.StatusCode);
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
            var databasePath = Path.Combine(Path.GetTempPath(), $"randwise-budgeting-{Guid.NewGuid():N}.db");
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
