using Microsoft.EntityFrameworkCore;
using RandWise.Application.Budgeting;
using RandWise.Application.Common;
using RandWise.Contracts.RecurringTransactions;
using RandWise.Domain.Entities;
using RandWise.Infrastructure.Persistence;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Infrastructure.Budgeting;

public sealed class EfRecurringTransactionService : IRecurringTransactionService
{
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly IIdGenerator idGenerator;

    public EfRecurringTransactionService(RandWiseDbContext dbContext, IClock clock, IIdGenerator idGenerator)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.idGenerator = idGenerator;
    }

    public async Task<IReadOnlyList<RecurringTransactionResponse>> ListAsync(
        string userId,
        CancellationToken cancellationToken) =>
        await dbContext.RecurringTransactions
            .AsNoTracking()
            .Where(recurring => recurring.UserId == userId)
            .OrderBy(recurring => recurring.NextOccurrenceDate)
            .ThenBy(recurring => recurring.Description)
            .Select(recurring => ToResponse(recurring))
            .ToListAsync(cancellationToken);

    public async Task<RecurringTransactionResponse> CreateAsync(
        string userId,
        RecurringTransactionRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCategoryAsync(userId, request.CategoryId, cancellationToken);
        var recurring = RecurringTransaction.Create(
            idGenerator.NewId(),
            userId,
            request.CategoryId,
            request.Description,
            request.Merchant,
            request.AmountInCents,
            DomainEnumNames.ParseTransactionType(request.TransactionType),
            DomainEnumNames.ParseRecurrenceFrequency(request.Frequency),
            request.DayOfMonth,
            string.IsNullOrWhiteSpace(request.DayOfWeek) ? null : DomainEnumNames.ParseDayOfWeek(request.DayOfWeek),
            request.NextOccurrenceDate,
            request.EndDate,
            request.AutoCreate,
            clock.UtcNow);

        dbContext.RecurringTransactions.Add(recurring);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(recurring);
    }

    public async Task<RecurringTransactionResponse> UpdateAsync(
        string userId,
        string id,
        RecurringTransactionRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCategoryAsync(userId, request.CategoryId, cancellationToken);
        var recurring = await GetOwnedAsync(userId, id, cancellationToken);
        recurring.UpdateDetails(
            request.CategoryId,
            request.Description,
            request.Merchant,
            request.AmountInCents,
            DomainEnumNames.ParseTransactionType(request.TransactionType),
            DomainEnumNames.ParseRecurrenceFrequency(request.Frequency),
            request.DayOfMonth,
            string.IsNullOrWhiteSpace(request.DayOfWeek) ? null : DomainEnumNames.ParseDayOfWeek(request.DayOfWeek),
            request.NextOccurrenceDate,
            request.EndDate,
            request.AutoCreate,
            clock.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(recurring);
    }

    public async Task DeleteAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var recurring = await GetOwnedAsync(userId, id, cancellationToken);
        dbContext.RecurringTransactions.Remove(recurring);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RecurringTransactionResponse> PauseAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var recurring = await GetOwnedAsync(userId, id, cancellationToken);
        recurring.Pause(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(recurring);
    }

    private async Task EnsureCategoryAsync(string userId, string categoryId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.BudgetCategories.AnyAsync(
            category => category.Id == categoryId
                && category.IsActive
                && (category.IsSystem || category.UserId == userId),
            cancellationToken);

        if (!exists)
        {
            throw new AppException(ApplicationError.Validation, "Category is invalid.");
        }
    }

    private async Task<RecurringTransaction> GetOwnedAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var recurring = await dbContext.RecurringTransactions.SingleOrDefaultAsync(
            recurring => recurring.Id == id && recurring.UserId == userId,
            cancellationToken);

        return recurring ?? throw new AppException(ApplicationError.NotFound, "Recurring transaction was not found.");
    }

    private static RecurringTransactionResponse ToResponse(RecurringTransaction recurring) =>
        new(
            recurring.Id,
            recurring.CategoryId,
            recurring.Description,
            recurring.Merchant,
            recurring.AmountCents,
            recurring.TransactionType.ToContract(),
            recurring.Frequency.ToContract(),
            recurring.DayOfMonth,
            recurring.DayOfWeek is null ? null : DomainEnumNames.ToContract(recurring.DayOfWeek.Value),
            recurring.NextOccurrenceDate,
            recurring.EndDate,
            recurring.AutoCreate,
            recurring.IsActive,
            recurring.CreatedUtc,
            recurring.UpdatedUtc);
}
