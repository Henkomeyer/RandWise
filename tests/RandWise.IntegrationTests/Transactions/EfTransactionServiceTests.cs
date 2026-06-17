using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RandWise.Application.Common;
using RandWise.Application.Transactions;
using RandWise.Contracts.Transactions;
using RandWise.Domain.Entities;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;
using RandWise.Infrastructure.Transactions;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.IntegrationTests.Transactions;

public sealed class EfTransactionServiceTests
{
    [Fact]
    public async Task CreateListUpdateDeleteRestoreAndCategorise_UseAuthenticatedUserScope()
    {
        await using var fixture = await TransactionServiceFixture.CreateAsync();
        var service = fixture.CreateService();

        await fixture.SeedUserAsync("user-one");
        await fixture.SeedUserAsync("user-two");
        await fixture.SeedCategoryAsync("category-food", "user-one", BudgetCategoryType.Expense);
        await fixture.SeedCategoryAsync("category-fuel", "user-one", BudgetCategoryType.Expense);
        await fixture.SeedCategoryAsync("category-other-user", "user-two", BudgetCategoryType.Expense);

        var created = await service.CreateAsync(
            "user-one",
            new CreateTransactionRequest(
                25000,
                "expense",
                "category-food",
                "Petrol",
                "Shell",
                new DateOnly(2026, 6, 14),
                "web"),
            CancellationToken.None);

        Assert.Equal(25000, created.AmountInCents);
        Assert.Equal("expense", created.TransactionType);
        Assert.Equal("web", created.Source);
        Assert.Equal("confirmed", created.Status);

        var otherUserList = await service.ListAsync(
            "user-two",
            new TransactionQuery(null, null, null, null, null, null, 1, 25),
            CancellationToken.None);
        Assert.Empty(otherUserList.Items);

        var filtered = await service.ListAsync(
            "user-one",
            new TransactionQuery(
                new DateOnly(2026, 6, 1),
                new DateOnly(2026, 6, 30),
                "category-food",
                "expense",
                "web",
                "Shell",
                1,
                25),
            CancellationToken.None);
        Assert.Single(filtered.Items);

        var updated = await service.UpdateAsync(
            "user-one",
            created.Id,
            new UpdateTransactionRequest(
                30000,
                "expense",
                "category-fuel",
                "Fuel top-up",
                "Shell",
                new DateOnly(2026, 6, 15),
                "Full tank"),
            CancellationToken.None);
        Assert.Equal(30000, updated.AmountInCents);
        Assert.Equal("category-fuel", updated.CategoryId);
        Assert.Equal("Full tank", updated.Notes);

        var categorised = await service.CategoriseAsync(
            "user-one",
            created.Id,
            new CategoriseTransactionRequest("category-food"),
            CancellationToken.None);
        Assert.Equal("category-food", categorised.CategoryId);

        await service.DeleteAsync("user-one", created.Id, CancellationToken.None);

        Assert.Null(await service.GetAsync("user-one", created.Id, CancellationToken.None));
        var afterDelete = await service.ListAsync(
            "user-one",
            new TransactionQuery(null, null, null, null, null, null, 1, 25),
            CancellationToken.None);
        Assert.Empty(afterDelete.Items);

        var restored = await service.RestoreAsync("user-one", created.Id, CancellationToken.None);
        Assert.Null(restored.DeletedUtc);
        Assert.Equal("confirmed", restored.Status);

        await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateAsync(
                "user-two",
                created.Id,
                new UpdateTransactionRequest(
                    1000,
                    "expense",
                    "category-other-user",
                    "Blocked",
                    null,
                    new DateOnly(2026, 6, 16),
                    null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_RejectsOtherUsersCategory()
    {
        await using var fixture = await TransactionServiceFixture.CreateAsync();
        var service = fixture.CreateService();

        await fixture.SeedUserAsync("user-one");
        await fixture.SeedUserAsync("user-two");
        await fixture.SeedCategoryAsync("category-other-user", "user-two", BudgetCategoryType.Expense);

        await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(
                "user-one",
                new CreateTransactionRequest(
                    1000,
                    "expense",
                    "category-other-user",
                    "Should fail",
                    null,
                    new DateOnly(2026, 6, 14),
                    "web"),
                CancellationToken.None));
    }

    private sealed class TransactionServiceFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly RandWiseDbContext context;
        private readonly FixedClock clock = new(new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc));

        private TransactionServiceFixture(SqliteConnection connection, RandWiseDbContext context)
        {
            this.connection = connection;
            this.context = context;
        }

        public static async Task<TransactionServiceFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<RandWiseDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new RandWiseDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new TransactionServiceFixture(connection, context);
        }

        public ITransactionService CreateService() =>
            new EfTransactionService(context, clock, new GuidIdGenerator());

        public async Task SeedUserAsync(string userId)
        {
            context.AppUsers.Add(AppUser.Create(userId, $"identity-{userId}", $"User {userId}", clock.UtcNow));
            await context.SaveChangesAsync();
        }

        public async Task SeedCategoryAsync(string categoryId, string userId, BudgetCategoryType categoryType)
        {
            context.BudgetCategories.Add(BudgetCategory.CreateUser(
                categoryId,
                userId,
                categoryId,
                categoryId,
                categoryType,
                null,
                0,
                clock.UtcNow));
            await context.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
