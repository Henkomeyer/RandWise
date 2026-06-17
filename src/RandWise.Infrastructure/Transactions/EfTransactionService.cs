using Microsoft.EntityFrameworkCore;
using RandWise.Application.Common;
using RandWise.Application.Transactions;
using RandWise.Contracts.Common;
using RandWise.Contracts.Transactions;
using RandWise.Domain.Entities;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Infrastructure.Transactions;

public sealed class EfTransactionService : ITransactionService
{
    private const string DefaultExpenseCategoryId = "system-expense-uncategorised";
    private const int AutoConfirmThresholdBasisPoints = 9000;
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly IIdGenerator idGenerator;

    public EfTransactionService(RandWiseDbContext dbContext, IClock clock, IIdGenerator idGenerator)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.idGenerator = idGenerator;
    }

    public async Task<TransactionResponse> CreateAsync(
        string userId,
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        return await CreateCoreAsync(userId, request, incomingMessageId: null, recurringTransactionId: null, confidenceBasisPoints: null, cancellationToken);
    }

    public async Task<TransactionResponse> CreateFromWhatsAppAsync(
        string userId,
        string incomingMessageId,
        CreateTransactionRequest request,
        int confidenceBasisPoints,
        CancellationToken cancellationToken)
    {
        return await CreateCoreAsync(userId, request, incomingMessageId, recurringTransactionId: null, confidenceBasisPoints, cancellationToken);
    }

    public async Task<TransactionResponse> CreateFromRecurringAsync(
        string userId,
        string recurringTransactionId,
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        return await CreateCoreAsync(userId, request, incomingMessageId: null, recurringTransactionId, confidenceBasisPoints: null, cancellationToken);
    }

    private async Task<TransactionResponse> CreateCoreAsync(
        string userId,
        CreateTransactionRequest request,
        string? incomingMessageId,
        string? recurringTransactionId,
        int? confidenceBasisPoints,
        CancellationToken cancellationToken)
    {
        var transactionType = DomainEnumNames.ParseTransactionType(request.TransactionType);
        var source = DomainEnumNames.ParseTransactionSource(request.Source);
        var categoryId = await ResolveCategoryIdAsync(userId, request.CategoryId, transactionType, cancellationToken);
        var now = clock.UtcNow;

        var transaction = Transaction.Create(
            idGenerator.NewId(),
            userId,
            categoryId,
            request.AmountInCents,
            transactionType,
            request.Description,
            request.Merchant,
            request.TransactionDate,
            source,
            confidenceBasisPoints is >= 7000 and < AutoConfirmThresholdBasisPoints ? TransactionStatus.NeedsReview : TransactionStatus.Confirmed,
            confidenceBasisPoints,
            now);

        if (!string.IsNullOrWhiteSpace(incomingMessageId))
        {
            transaction.LinkIncomingMessage(incomingMessageId);
        }

        if (!string.IsNullOrWhiteSpace(recurringTransactionId))
        {
            transaction.LinkRecurringTransaction(recurringTransactionId);
        }

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(transaction);
    }

    public async Task<PagedResponse<TransactionResponse>> ListAsync(
        string userId,
        TransactionQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var transactions = ApplyFilters(
            dbContext.Transactions.AsNoTracking().Where(transaction => transaction.UserId == userId && transaction.DeletedUtc == null),
            query);

        var totalCount = await transactions.CountAsync(cancellationToken);
        var items = await transactions
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.CreatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(transaction => ToResponse(transaction))
            .ToListAsync(cancellationToken);

        return new PagedResponse<TransactionResponse>(
            items,
            page,
            pageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<TransactionResponse?> GetAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                transaction => transaction.Id == id && transaction.UserId == userId && transaction.DeletedUtc == null,
                cancellationToken);

        return transaction is null ? null : ToResponse(transaction);
    }

    public async Task<TransactionResponse> UpdateAsync(
        string userId,
        string id,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await GetOwnedTransactionAsync(userId, id, includeDeleted: false, cancellationToken);
        var transactionType = DomainEnumNames.ParseTransactionType(request.TransactionType);
        var categoryId = await ResolveCategoryIdAsync(userId, request.CategoryId, transactionType, cancellationToken);

        transaction.Update(
            categoryId,
            request.AmountInCents,
            transactionType,
            request.Description,
            request.Merchant,
            request.TransactionDate,
            request.Notes,
            clock.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(transaction);
    }

    public async Task DeleteAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var transaction = await GetOwnedTransactionAsync(userId, id, includeDeleted: false, cancellationToken);
        transaction.MarkDeleted(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TransactionResponse> RestoreAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var transaction = await GetOwnedTransactionAsync(userId, id, includeDeleted: true, cancellationToken);
        transaction.Restore(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(transaction);
    }

    public async Task<TransactionResponse> CategoriseAsync(
        string userId,
        string id,
        CategoriseTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await GetOwnedTransactionAsync(userId, id, includeDeleted: false, cancellationToken);
        var category = await dbContext.BudgetCategories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                category => category.Id == request.CategoryId
                    && category.IsActive
                    && (category.IsSystem || category.UserId == userId),
                cancellationToken);

        if (category is null)
        {
            throw new AppException(ApplicationError.Validation, "Category is invalid.");
        }

        transaction.Update(
            category.Id,
            transaction.AmountCents,
            transaction.TransactionType,
            transaction.Description,
            transaction.Merchant,
            transaction.TransactionDate,
            transaction.Notes,
            clock.UtcNow);

        transaction.MarkConfirmed(clock.UtcNow);

        if (request.CreateRule)
        {
            await LearnCategoryRuleAsync(userId, transaction, request, category.Id, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(transaction);
    }

    private async Task LearnCategoryRuleAsync(
        string userId,
        Transaction transaction,
        CategoriseTransactionRequest request,
        string categoryId,
        CancellationToken cancellationToken)
    {
        var matchType = ParseMatchType(request.MatchType);
        var matchValue = ResolveRuleMatchValue(transaction, request.MatchValue, matchType);
        var normalizedMatchValue = matchValue.Trim().ToLowerInvariant();

        var activeDuplicate = await dbContext.UserCategoryRules
            .Where(rule => rule.UserId == userId
                && rule.IsActive
                && rule.MatchType == matchType
                && rule.NormalizedMatchValue == normalizedMatchValue)
            .OrderByDescending(rule => rule.Priority)
            .ThenByDescending(rule => rule.UpdatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeDuplicate?.CategoryId == categoryId)
        {
            return;
        }

        activeDuplicate?.Deactivate(clock.UtcNow);

        dbContext.UserCategoryRules.Add(UserCategoryRule.Create(
            idGenerator.NewId(),
            userId,
            matchType,
            matchValue,
            categoryId,
            200,
            clock.UtcNow));
    }

    private static CategoryRuleMatchType ParseMatchType(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            null or "" => CategoryRuleMatchType.Keyword,
            "keyword" => CategoryRuleMatchType.Keyword,
            "merchant" => CategoryRuleMatchType.Merchant,
            _ => throw new AppException(ApplicationError.Validation, "Rule match type is invalid.")
        };

    private static string ResolveRuleMatchValue(
        Transaction transaction,
        string? requestedMatchValue,
        CategoryRuleMatchType matchType)
    {
        if (!string.IsNullOrWhiteSpace(requestedMatchValue))
        {
            return requestedMatchValue;
        }

        if (matchType == CategoryRuleMatchType.Merchant && !string.IsNullOrWhiteSpace(transaction.Merchant))
        {
            return transaction.Merchant;
        }

        return transaction.Description;
    }

    private static IQueryable<Transaction> ApplyFilters(IQueryable<Transaction> transactions, TransactionQuery query)
    {
        if (query.From is not null)
        {
            transactions = transactions.Where(transaction => transaction.TransactionDate >= query.From.Value);
        }

        if (query.To is not null)
        {
            transactions = transactions.Where(transaction => transaction.TransactionDate <= query.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CategoryId))
        {
            transactions = transactions.Where(transaction => transaction.CategoryId == query.CategoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            var type = DomainEnumNames.ParseTransactionType(query.Type);
            transactions = transactions.Where(transaction => transaction.TransactionType == type);
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            var source = DomainEnumNames.ParseTransactionSource(query.Source);
            transactions = transactions.Where(transaction => transaction.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            transactions = transactions.Where(transaction =>
                transaction.Description.Contains(search)
                || (transaction.Merchant != null && transaction.Merchant.Contains(search))
                || (transaction.Notes != null && transaction.Notes.Contains(search)));
        }

        return transactions;
    }

    private async Task<Transaction> GetOwnedTransactionAsync(
        string userId,
        string id,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Transactions.Where(transaction => transaction.Id == id && transaction.UserId == userId);

        if (!includeDeleted)
        {
            query = query.Where(transaction => transaction.DeletedUtc == null);
        }

        var transaction = await query.SingleOrDefaultAsync(cancellationToken);

        return transaction ?? throw new AppException(ApplicationError.NotFound, "Transaction was not found.");
    }

    private async Task<string> ResolveCategoryIdAsync(
        string userId,
        string? categoryId,
        TransactionType transactionType,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(categoryId))
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

            return categoryId;
        }

        var defaultCategory = await dbContext.BudgetCategories.FindAsync([DefaultExpenseCategoryId], cancellationToken);
        if (defaultCategory is null)
        {
            defaultCategory = BudgetCategory.CreateSystem(
                DefaultExpenseCategoryId,
                "Uncategorised",
                "uncategorised",
                transactionType == TransactionType.Income ? BudgetCategoryType.Income : BudgetCategoryType.Expense,
                null,
                0,
                clock.UtcNow);
            dbContext.BudgetCategories.Add(defaultCategory);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return defaultCategory.Id;
    }

    private static TransactionResponse ToResponse(Transaction transaction) =>
        new(
            transaction.Id,
            transaction.AmountCents,
            transaction.TransactionType.ToContract(),
            transaction.CategoryId,
            transaction.Description,
            transaction.Merchant,
            transaction.TransactionDate,
            transaction.Source.ToContract(),
            transaction.Status.ToContract(),
            transaction.Notes,
            transaction.CreatedUtc,
            transaction.UpdatedUtc,
            transaction.DeletedUtc);
}
