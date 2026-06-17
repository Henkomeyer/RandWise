using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RandWise.Domain.Entities;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;

namespace RandWise.IntegrationTests.Persistence;

public sealed class RandWiseDbContextMigrationTests
{
    [Fact]
    public async Task MigrateAsync_CreatesCoreTablesIndexesAndEnablesWal()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();

        await using var connection = new SqliteConnection(database.ConnectionString);
        await connection.OpenAsync();

        var tables = await ReadStringsAsync(
            connection,
            "SELECT name FROM sqlite_schema WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;");
        var indexes = await ReadStringsAsync(
            connection,
            "SELECT name FROM sqlite_schema WHERE type = 'index' AND name NOT LIKE 'sqlite_%' ORDER BY name;");

        Assert.Contains("AppUsers", tables);
        Assert.Contains("FinancialProfiles", tables);
        Assert.Contains("BudgetPeriods", tables);
        Assert.Contains("BudgetCategories", tables);
        Assert.Contains("CategoryBudgets", tables);
        Assert.Contains("Transactions", tables);
        Assert.Contains("RecurringTransactions", tables);
        Assert.Contains("WhatsAppContacts", tables);
        Assert.Contains("IncomingMessages", tables);
        Assert.Contains("MessageInterpretations", tables);
        Assert.Contains("UserCategoryRules", tables);
        Assert.Contains("Notifications", tables);
        Assert.Contains("AuditLogs", tables);

        Assert.Contains("IX_AppUsers_IdentityUserId", indexes);
        Assert.Contains("IX_FinancialProfiles_UserId", indexes);
        Assert.Contains("IX_BudgetPeriods_UserId_StartDate_EndDate", indexes);
        Assert.Contains("IX_CategoryBudgets_BudgetPeriodId_CategoryId", indexes);
        Assert.Contains("IX_WhatsAppContacts_PhoneNumberHash", indexes);
        Assert.Contains("IX_IncomingMessages_WhatsAppMessageId", indexes);
        Assert.Contains("IX_MessageInterpretations_IncomingMessageId", indexes);

        await using var journalCommand = connection.CreateCommand();
        journalCommand.CommandText = "PRAGMA journal_mode;";
        var journalMode = Assert.IsType<string>(await journalCommand.ExecuteScalarAsync());
        Assert.Equal("wal", journalMode, ignoreCase: true);
    }

    [Fact]
    public async Task IncomingMessage_WhatsAppMessageId_IsUniqueForWebhookIdempotency()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var receivedUtc = new DateTime(2026, 06, 14, 10, 00, 00, DateTimeKind.Utc);

        await using (var context = database.CreateContext())
        {
            context.IncomingMessages.Add(IncomingMessage.Create(
                "message-1",
                null,
                "wamid.same-platform-id",
                "platform-contact-1",
                "text",
                "encrypted-body-1",
                "payload-hash-1",
                receivedUtc));

            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            context.IncomingMessages.Add(IncomingMessage.Create(
                "message-2",
                null,
                "wamid.same-platform-id",
                "platform-contact-1",
                "text",
                "encrypted-body-2",
                "payload-hash-2",
                receivedUtc.AddSeconds(1)));

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task UserOwnedSchema_AllowsLaterAuthorizationFilteringColumns()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var createdUtc = new DateTime(2026, 06, 14, 10, 00, 00, DateTimeKind.Utc);

        await using var context = database.CreateContext();
        context.AppUsers.Add(AppUser.Create("user-1", "identity-1", "Test User", createdUtc));
        context.BudgetCategories.Add(BudgetCategory.CreateUser(
            "category-1",
            "user-1",
            "Groceries",
            "groceries",
            BudgetCategoryType.Expense,
            null,
            1,
            createdUtc));
        context.Transactions.Add(Transaction.Create(
            "transaction-1",
            "user-1",
            "category-1",
            25000,
            TransactionType.Expense,
            "Petrol",
            "Shell",
            new DateOnly(2026, 06, 14),
            TransactionSource.Web,
            TransactionStatus.Confirmed,
            null,
            createdUtc));

        await context.SaveChangesAsync();

        var transaction = await context.Transactions.SingleAsync(transaction => transaction.UserId == "user-1");
        Assert.Equal("transaction-1", transaction.Id);
        Assert.Equal(25000, transaction.AmountCents);
    }

    private static async Task<string[]> ReadStringsAsync(SqliteConnection connection, string sql)
    {
        var results = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        return results.ToArray();
    }

    private sealed class SqliteTestDatabase : IAsyncDisposable
    {
        private readonly string databasePath;

        private SqliteTestDatabase(string databasePath)
        {
            this.databasePath = databasePath;
            ConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath
            }.ToString();
        }

        public string ConnectionString { get; }

        public static async Task<SqliteTestDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"randwise-{Guid.NewGuid():N}.db");
            var database = new SqliteTestDatabase(databasePath);

            await using var context = database.CreateContext();
            await context.Database.MigrateAsync();

            return database;
        }

        public RandWiseDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<RandWiseDbContext>()
                .UseSqlite(ConnectionString)
                .AddInterceptors(new SqliteWalConnectionInterceptor())
                .Options;

            return new RandWiseDbContext(options);
        }

        public ValueTask DisposeAsync()
        {
            TryDelete(databasePath);
            TryDelete($"{databasePath}-shm");
            TryDelete($"{databasePath}-wal");

            return ValueTask.CompletedTask;
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
