using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RandWise.Application.Common;
using RandWise.Application.Jobs;
using RandWise.Application.WhatsApp;
using RandWise.Contracts.Auth;
using RandWise.Contracts.Categories;
using RandWise.Contracts.CategoryRules;
using RandWise.Contracts.Common;
using RandWise.Contracts.FinancialProfile;
using RandWise.Contracts.RecurringTransactions;
using RandWise.Contracts.Transactions;
using RandWise.Contracts.WhatsApp;
using RandWise.Domain.Entities;
using RandWise.Infrastructure.Persistence;

namespace RandWise.IntegrationTests.WhatsApp;

public sealed class WhatsAppEndpointIntegrationTests
{
    private const string WebhookSecret = "integration-webhook-secret";

    [Fact]
    public async Task LinkStatusAndUnlink_ManageVerifiedWhatsAppContact()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await RegisterAsync(client, "wa-link@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        using var statusBeforeResponse = await client.GetAsync("/api/v1/whatsapp/status");
        statusBeforeResponse.EnsureSuccessStatusCode();
        var statusBefore = await ReadAsync<WhatsAppStatusResponse>(statusBeforeResponse);
        Assert.False(statusBefore.IsLinked);

        using var linkResponse = await client.PostAsJsonAsync(
            "/api/v1/whatsapp/link",
            new LinkWhatsAppRequest("+27 82 555 0101", "wa-contact-1"));
        linkResponse.EnsureSuccessStatusCode();
        var linked = await ReadAsync<WhatsAppStatusResponse>(linkResponse);
        Assert.True(linked.IsLinked);
        Assert.True(linked.IsVerified);
        Assert.Equal("wa-contact-1", linked.PlatformContactId);

        await factory.UseContextAsync(async context =>
        {
            var contact = await context.WhatsAppContacts.SingleAsync();
            Assert.StartsWith("sha256:", contact.PhoneNumberHash, StringComparison.Ordinal);
            Assert.DoesNotContain("082", contact.EncryptedPhoneNumber, StringComparison.Ordinal);
        });

        using var unlinkResponse = await client.PostAsync("/api/v1/whatsapp/unlink", null);
        Assert.Equal(HttpStatusCode.NoContent, unlinkResponse.StatusCode);

        using var statusAfterResponse = await client.GetAsync("/api/v1/whatsapp/status");
        statusAfterResponse.EnsureSuccessStatusCode();
        var statusAfter = await ReadAsync<WhatsAppStatusResponse>(statusAfterResponse);
        Assert.False(statusAfter.IsLinked);
    }

    [Fact]
    public async Task SignedWebhook_PersistsIncomingMessageAndIgnoresDuplicate()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await RegisterAsync(client, "wa-webhook@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        await ConfigureNotificationModeAsync(client, "confirm");

        using var linkResponse = await client.PostAsJsonAsync(
            "/api/v1/whatsapp/link",
            new LinkWhatsAppRequest("+27 82 555 0202", "wa-contact-2"));
        linkResponse.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;

        var body = """
            {
              "messageId": "wamid.123",
              "platformContactId": "wa-contact-2",
              "fromPhoneNumber": "+27825550202",
              "messageType": "text",
              "text": "R250 petrol",
              "receivedUtc": "2026-06-17T10:00:00Z"
            }
            """;

        using var firstResponse = await PostWebhookAsync(client, body);
        firstResponse.EnsureSuccessStatusCode();
        var first = await ReadAsync<WhatsAppWebhookIngestionResponse>(firstResponse);
        Assert.True(first.Accepted);
        Assert.False(first.Duplicate);

        using var duplicateResponse = await PostWebhookAsync(client, body);
        duplicateResponse.EnsureSuccessStatusCode();
        var duplicate = await ReadAsync<WhatsAppWebhookIngestionResponse>(duplicateResponse);
        Assert.True(duplicate.Duplicate);

        await factory.UseContextAsync(async context =>
        {
            var message = await context.IncomingMessages.SingleAsync();
            var appUser = await context.AppUsers.SingleAsync();
            var interpretation = await context.MessageInterpretations.SingleAsync();
            var transaction = await context.Transactions.SingleAsync();
            var notification = await context.Notifications.SingleAsync();

            Assert.Equal(appUser.Id, message.UserId);
            Assert.Equal("wamid.123", message.WhatsAppMessageId);
            Assert.Equal("wa-contact-2", message.PlatformContactId);
            Assert.Equal("Processed", message.ProcessingStatus.ToString());
            Assert.StartsWith("sha256:", message.PayloadHash, StringComparison.Ordinal);
            Assert.DoesNotContain("R250 petrol", message.RawTextEncrypted, StringComparison.Ordinal);
            Assert.Equal(message.Id, interpretation.IncomingMessageId);
            Assert.Equal("create-transaction", interpretation.Intent);
            Assert.Equal(25000, interpretation.AmountCents);
            Assert.Equal(message.Id, transaction.IncomingMessageId);
            Assert.Equal(25000, transaction.AmountCents);
            Assert.Equal("Petrol", transaction.Description);
            Assert.Equal("WhatsApp", transaction.Source.ToString());
            Assert.Equal("TransactionConfirmation", notification.NotificationType.ToString());
            Assert.Equal("WhatsApp", notification.Channel.ToString());
            Assert.Equal("Sent", notification.Status.ToString());
            Assert.DoesNotContain("Added R250", notification.MessageEncrypted, StringComparison.Ordinal);
        });

    }

    [Fact]
    public async Task WebhookCreatedTransaction_IsVisibleThroughTransactionApiAsWhatsAppSource()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await RegisterAsync(client, "wa-transaction-api@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        await ConfigureNotificationModeAsync(client, "silent");

        using var linkResponse = await client.PostAsJsonAsync(
            "/api/v1/whatsapp/link",
            new LinkWhatsAppRequest("+27 82 555 0303", "wa-contact-3"));
        linkResponse.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;

        var body = """
            {
              "messageId": "wamid.456",
              "platformContactId": "wa-contact-3",
              "fromPhoneNumber": "+27825550303",
              "messageType": "text",
              "text": "spent R250 on petrol",
              "receivedUtc": "2026-06-17T10:00:00Z"
            }
            """;

        using var webhookResponse = await PostWebhookAsync(client, body);
        webhookResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        using var listResponse = await client.GetAsync("/api/v1/transactions?source=whatsapp");
        listResponse.EnsureSuccessStatusCode();
        var page = await ReadAsync<PagedResponse<TransactionResponse>>(listResponse);

        var transaction = Assert.Single(page.Items);
        Assert.Equal(25000, transaction.AmountInCents);
        Assert.Equal("Petrol", transaction.Description);
        Assert.Equal("whatsapp", transaction.Source);
        Assert.Equal("needsReview", transaction.Status);

        await factory.UseContextAsync(async context =>
        {
            Assert.Empty(await context.Notifications.ToListAsync());
        });
    }

    [Fact]
    public async Task PersonalCategoryRule_CategorisesWhatsAppTransaction()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await RegisterAsync(client, "wa-personal-rule@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var category = await CreateCategoryAsync(client, "Fuel");
        using var ruleResponse = await client.PostAsJsonAsync(
            "/api/v1/category-rules",
            new CategoryRuleRequest("keyword", "petrol", category.Id, 100));
        ruleResponse.EnsureSuccessStatusCode();
        var rule = await ReadAsync<CategoryRuleResponse>(ruleResponse);
        Assert.Equal(category.Id, rule.CategoryId);

        using var linkResponse = await client.PostAsJsonAsync(
            "/api/v1/whatsapp/link",
            new LinkWhatsAppRequest("+27 82 555 0505", "wa-contact-5"));
        linkResponse.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;

        using var webhookResponse = await PostWebhookAsync(client, """
            {
              "messageId": "wamid.personal-rule",
              "platformContactId": "wa-contact-5",
              "fromPhoneNumber": "+27825550505",
              "messageType": "text",
              "text": "R450 petrol",
              "receivedUtc": "2026-06-17T10:00:00Z"
            }
            """);
        webhookResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        using var listResponse = await client.GetAsync("/api/v1/transactions?source=whatsapp");
        listResponse.EnsureSuccessStatusCode();
        var page = await ReadAsync<PagedResponse<TransactionResponse>>(listResponse);
        var transaction = Assert.Single(page.Items);
        Assert.Equal(category.Id, transaction.CategoryId);
        Assert.Equal("confirmed", transaction.Status);
    }

    [Fact]
    public async Task SystemMerchantRule_CategorisesKnownMerchant()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await RegisterAsync(client, "wa-system-rule@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var petrolCategory = await CreateCategoryAsync(client, "Petrol");
        using var linkResponse = await client.PostAsJsonAsync(
            "/api/v1/whatsapp/link",
            new LinkWhatsAppRequest("+27 82 555 0606", "wa-contact-6"));
        linkResponse.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;

        using var webhookResponse = await PostWebhookAsync(client, """
            {
              "messageId": "wamid.system-rule",
              "platformContactId": "wa-contact-6",
              "fromPhoneNumber": "+27825550606",
              "messageType": "text",
              "text": "R300 Shell",
              "receivedUtc": "2026-06-17T10:00:00Z"
            }
            """);
        webhookResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        using var listResponse = await client.GetAsync("/api/v1/transactions?source=whatsapp");
        listResponse.EnsureSuccessStatusCode();
        var page = await ReadAsync<PagedResponse<TransactionResponse>>(listResponse);
        var transaction = Assert.Single(page.Items);
        Assert.Equal(petrolCategory.Id, transaction.CategoryId);
        Assert.Equal("confirmed", transaction.Status);
    }

    [Fact]
    public async Task CategoryCorrection_CreatesPersonalRuleAndConfirmsTransaction()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await RegisterAsync(client, "wa-correction-rule@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var fuelCategory = await CreateCategoryAsync(client, "Fuel");

        using var linkResponse = await client.PostAsJsonAsync(
            "/api/v1/whatsapp/link",
            new LinkWhatsAppRequest("+27 82 555 0707", "wa-contact-7"));
        linkResponse.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;

        using var firstWebhookResponse = await PostWebhookAsync(client, """
            {
              "messageId": "wamid.correction-source",
              "platformContactId": "wa-contact-7",
              "fromPhoneNumber": "+27825550707",
              "messageType": "text",
              "text": "R450 petrol",
              "receivedUtc": "2026-06-17T10:00:00Z"
            }
            """);
        firstWebhookResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        using var listBeforeResponse = await client.GetAsync("/api/v1/transactions?source=whatsapp");
        listBeforeResponse.EnsureSuccessStatusCode();
        var beforePage = await ReadAsync<PagedResponse<TransactionResponse>>(listBeforeResponse);
        var transaction = Assert.Single(beforePage.Items);
        Assert.Equal("needsReview", transaction.Status);

        using var categoriseResponse = await client.PostAsJsonAsync(
            $"/api/v1/transactions/{transaction.Id}/categorise",
            new CategoriseTransactionRequest(fuelCategory.Id, true, "keyword", "petrol"));
        categoriseResponse.EnsureSuccessStatusCode();
        var corrected = await ReadAsync<TransactionResponse>(categoriseResponse);
        Assert.Equal(fuelCategory.Id, corrected.CategoryId);
        Assert.Equal("confirmed", corrected.Status);

        using var rulesResponse = await client.GetAsync("/api/v1/category-rules");
        rulesResponse.EnsureSuccessStatusCode();
        var rules = await ReadAsync<List<CategoryRuleResponse>>(rulesResponse);
        var learnedRule = Assert.Single(rules);
        Assert.Equal("keyword", learnedRule.MatchType);
        Assert.Equal("petrol", learnedRule.MatchValue);
        Assert.Equal(fuelCategory.Id, learnedRule.CategoryId);

        client.DefaultRequestHeaders.Authorization = null;
        using var secondWebhookResponse = await PostWebhookAsync(client, """
            {
              "messageId": "wamid.correction-learned",
              "platformContactId": "wa-contact-7",
              "fromPhoneNumber": "+27825550707",
              "messageType": "text",
              "text": "R100 petrol",
              "receivedUtc": "2026-06-18T10:00:00Z"
            }
            """);
        secondWebhookResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        using var listAfterResponse = await client.GetAsync("/api/v1/transactions?source=whatsapp&search=Petrol");
        listAfterResponse.EnsureSuccessStatusCode();
        var afterPage = await ReadAsync<PagedResponse<TransactionResponse>>(listAfterResponse);
        Assert.All(afterPage.Items, item => Assert.Equal(fuelCategory.Id, item.CategoryId));
        Assert.Contains(afterPage.Items, item => item.AmountInCents == 10000 && item.Status == "confirmed");
    }

    [Fact]
    public async Task BackgroundJobProcessor_RetriesAndDeadLettersFailingWhatsAppMessages()
    {
        await using var factory = await TestApplication.CreateAsync(services =>
        {
            services.RemoveAll<IWhatsAppMessageProcessor>();
            services.AddScoped<IWhatsAppMessageProcessor, ThrowingWhatsAppMessageProcessor>();
        });

        await factory.UseContextAsync(async context =>
        {
            context.IncomingMessages.Add(IncomingMessage.Create(
                "incoming-retry",
                "user-retry",
                "wamid.retry",
                "wa-contact-retry",
                "text",
                "protected",
                "sha256:payload",
                new DateTime(2026, 6, 17, 10, 0, 0, DateTimeKind.Utc)));
            context.AppUsers.Add(AppUser.Create(
                "user-retry",
                "identity-user-retry",
                "Retry User",
                new DateTime(2026, 6, 17, 10, 0, 0, DateTimeKind.Utc)));
            await context.SaveChangesAsync();
        });

        Assert.Equal(1, await factory.ProcessQueuedWhatsAppAsync());

        await factory.UseContextAsync(async context =>
        {
            var message = await context.IncomingMessages.SingleAsync();
            Assert.Equal("Received", message.ProcessingStatus.ToString());
            Assert.Equal(1, message.AttemptCount);
            Assert.Contains("InvalidOperationException", message.FailureReason, StringComparison.Ordinal);
        });

        Assert.Equal(1, await factory.ProcessQueuedWhatsAppAsync());
        Assert.Equal(1, await factory.ProcessQueuedWhatsAppAsync());

        await factory.UseContextAsync(async context =>
        {
            var message = await context.IncomingMessages.SingleAsync();
            Assert.Equal("Failed", message.ProcessingStatus.ToString());
            Assert.Equal(3, message.AttemptCount);
            Assert.Contains("InvalidOperationException", message.FailureReason, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task BackgroundJobProcessor_GeneratesDueRecurringTransactions()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await RegisterAsync(client, "recurring-job@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var today = new DateOnly(2026, 06, 17);
        var category = await CreateCategoryAsync(client, "Internet");

        using var recurringResponse = await client.PostAsJsonAsync(
            "/api/v1/recurring-transactions",
            new RecurringTransactionRequest(
                category.Id,
                "Fibre",
                "Fibre Co",
                79900,
                "expense",
                "monthly",
                today.Day,
                null,
                today,
                null,
                true));
        recurringResponse.EnsureSuccessStatusCode();

        var generated = await factory.GenerateRecurringAsync(today);
        Assert.Equal(1, generated);

        using var listResponse = await client.GetAsync("/api/v1/transactions?source=recurring");
        listResponse.EnsureSuccessStatusCode();
        var page = await ReadAsync<PagedResponse<TransactionResponse>>(listResponse);
        var transaction = Assert.Single(page.Items);
        Assert.Equal("Fibre", transaction.Description);
        Assert.Equal("recurring", transaction.Source);

        await factory.UseContextAsync(async context =>
        {
            var recurring = await context.RecurringTransactions.SingleAsync();
            Assert.Equal(today.AddMonths(1), recurring.NextOccurrenceDate);
        });
    }

    [Fact]
    public async Task CoachMode_CreatesSentCoachingConfirmation()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await RegisterAsync(client, "wa-coach@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        await ConfigureNotificationModeAsync(client, "coach");

        using var linkResponse = await client.PostAsJsonAsync(
            "/api/v1/whatsapp/link",
            new LinkWhatsAppRequest("+27 82 555 0404", "wa-contact-4"));
        linkResponse.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;

        var body = """
            {
              "messageId": "wamid.789",
              "platformContactId": "wa-contact-4",
              "fromPhoneNumber": "+27825550404",
              "messageType": "text",
              "text": "R99.95 lunch",
              "receivedUtc": "2026-06-17T10:00:00Z"
            }
            """;

        using var webhookResponse = await PostWebhookAsync(client, body);
        webhookResponse.EnsureSuccessStatusCode();

        await factory.UseContextAsync(async context =>
        {
            var notification = await context.Notifications.SingleAsync();

            Assert.Equal("Sent", notification.Status.ToString());
            Assert.Equal("TransactionConfirmation", notification.NotificationType.ToString());
            Assert.DoesNotContain("dashboard", notification.MessageEncrypted, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Webhook_RejectsInvalidSignature()
    {
        await using var factory = await TestApplication.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/whatsapp")
        {
            Content = new StringContent("""{"messageId":"bad","platformContactId":"wa","messageType":"text"}""", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Hub-Signature-256", "sha256:invalid");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostWebhookAsync(HttpClient client, string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/whatsapp")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Hub-Signature-256", Sign(body));
        return await client.SendAsync(request);
    }

    private static string Sign(string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        return $"sha256={Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant()}";
    }

    private static async Task<AuthTokenResponse> RegisterAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "CorrectHorseBatteryStaple1", "WhatsApp User"));

        response.EnsureSuccessStatusCode();
        return await ReadAsync<AuthTokenResponse>(response);
    }

    private static async Task ConfigureNotificationModeAsync(HttpClient client, string notificationMode)
    {
        using var response = await client.PutAsJsonAsync(
            "/api/v1/financial-profile",
            new FinancialProfileRequest(
                2500000,
                25,
                "paydayToPayday",
                150000,
                50000,
                100000,
                notificationMode,
                "Monday"));

        response.EnsureSuccessStatusCode();
    }

    private static async Task<CategoryResponse> CreateCategoryAsync(HttpClient client, string name)
    {
        using var categoryResponse = await client.PostAsJsonAsync(
            "/api/v1/categories",
            new CategoryRequest(name, "expense", null, 10));
        categoryResponse.EnsureSuccessStatusCode();
        return await ReadAsync<CategoryResponse>(categoryResponse);
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

        public static async Task<TestApplication> CreateAsync(Action<IServiceCollection>? configureServices = null)
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"randwise-whatsapp-{Guid.NewGuid():N}.db");
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
                    builder.UseSetting("WhatsApp:VerifyToken", "verify-token");
                    builder.UseSetting("WhatsApp:AppSecret", WebhookSecret);
                    builder.UseSetting("SensitiveData:Key", "integration-sensitive-data-key-at-least-32-chars");
                    if (configureServices is not null)
                    {
                        builder.ConfigureServices(configureServices);
                    }
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

        public async Task<int> GenerateRecurringAsync(DateOnly today)
        {
            using var scope = factory.Services.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IBackgroundJobProcessor>();
            return await processor.GenerateDueRecurringTransactionsAsync(today, CancellationToken.None);
        }

        public async Task<int> ProcessQueuedWhatsAppAsync()
        {
            using var scope = factory.Services.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IBackgroundJobProcessor>();
            return await processor.ProcessQueuedWhatsAppMessagesAsync(CancellationToken.None);
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

    private sealed class ThrowingWhatsAppMessageProcessor : IWhatsAppMessageProcessor
    {
        private readonly RandWiseDbContext dbContext;
        private readonly IClock clock;

        public ThrowingWhatsAppMessageProcessor(RandWiseDbContext dbContext, IClock clock)
        {
            this.dbContext = dbContext;
            this.clock = clock;
        }

        public async Task ProcessAsync(string incomingMessageId, CancellationToken cancellationToken)
        {
            var incoming = await dbContext.IncomingMessages.SingleAsync(
                message => message.Id == incomingMessageId,
                cancellationToken);
            incoming.RecordProcessingAttempt(clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException("Simulated transient processor failure.");
        }
    }
}
