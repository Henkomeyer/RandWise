using Microsoft.EntityFrameworkCore;
using RandWise.Application.Budgeting;
using RandWise.Application.Common;
using RandWise.Contracts.Budgeting;
using RandWise.Domain.Entities;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Infrastructure.Budgeting;

public sealed class EfCategoryBudgetService : ICategoryBudgetService
{
    private readonly RandWiseDbContext dbContext;
    private readonly IClock clock;
    private readonly IIdGenerator idGenerator;

    public EfCategoryBudgetService(RandWiseDbContext dbContext, IClock clock, IIdGenerator idGenerator)
    {
        this.dbContext = dbContext;
        this.clock = clock;
        this.idGenerator = idGenerator;
    }

    public async Task<IReadOnlyList<CategoryBudgetResponse>> ListAsync(
        string userId,
        string budgetPeriodId,
        CancellationToken cancellationToken)
    {
        var period = await GetOwnedPeriodAsync(userId, budgetPeriodId, cancellationToken);
        var budgets = await dbContext.CategoryBudgets
            .AsNoTracking()
            .Where(budget => budget.BudgetPeriodId == period.Id)
            .Join(
                dbContext.BudgetCategories.AsNoTracking(),
                budget => budget.CategoryId,
                category => category.Id,
                (budget, category) => new { budget, category })
            .OrderBy(row => row.category.SortOrder)
            .ThenBy(row => row.category.Name)
            .ToListAsync(cancellationToken);

        var spent = await GetSpentByCategoryAsync(userId, period.StartDate, period.EndDate, cancellationToken);
        return budgets.Select(row => ToResponse(row.budget, row.category, spent)).ToList();
    }

    public async Task<CategoryBudgetResponse> CreateAsync(
        string userId,
        string budgetPeriodId,
        CategoryBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var period = await GetOwnedPeriodAsync(userId, budgetPeriodId, cancellationToken);
        var category = await GetUsableCategoryAsync(userId, request.CategoryId, cancellationToken);
        var exists = await dbContext.CategoryBudgets.AnyAsync(
            budget => budget.BudgetPeriodId == period.Id && budget.CategoryId == category.Id,
            cancellationToken);

        if (exists)
        {
            throw new AppException(ApplicationError.Validation, "Category budget already exists.");
        }

        var budget = CategoryBudget.Create(
            idGenerator.NewId(),
            period.Id,
            category.Id,
            request.AllocatedAmountCents,
            request.RolloverAmountCents,
            request.WarningThresholdPercent,
            clock.UtcNow);

        dbContext.CategoryBudgets.Add(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
        var spent = await GetSpentByCategoryAsync(userId, period.StartDate, period.EndDate, cancellationToken);
        return ToResponse(budget, category, spent);
    }

    public async Task<CategoryBudgetResponse> UpdateAsync(
        string userId,
        string id,
        CategoryBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var budget = await GetOwnedCategoryBudgetAsync(userId, id, cancellationToken);
        var period = await GetOwnedPeriodAsync(userId, budget.BudgetPeriodId, cancellationToken);
        var category = await GetUsableCategoryAsync(userId, request.CategoryId, cancellationToken);

        var duplicate = await dbContext.CategoryBudgets.AnyAsync(
            other => other.Id != id && other.BudgetPeriodId == period.Id && other.CategoryId == category.Id,
            cancellationToken);

        if (duplicate)
        {
            throw new AppException(ApplicationError.Validation, "Category budget already exists.");
        }

        budget.Update(
            category.Id,
            request.AllocatedAmountCents,
            request.RolloverAmountCents,
            request.WarningThresholdPercent,
            clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        var spent = await GetSpentByCategoryAsync(userId, period.StartDate, period.EndDate, cancellationToken);
        return ToResponse(budget, category, spent);
    }

    public async Task DeleteAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var budget = await GetOwnedCategoryBudgetAsync(userId, id, cancellationToken);
        dbContext.CategoryBudgets.Remove(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<BudgetPeriod> GetOwnedPeriodAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var period = await dbContext.BudgetPeriods.SingleOrDefaultAsync(
            period => period.Id == id && period.UserId == userId,
            cancellationToken);

        return period ?? throw new AppException(ApplicationError.NotFound, "Budget period was not found.");
    }

    private async Task<BudgetCategory> GetUsableCategoryAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var category = await dbContext.BudgetCategories.AsNoTracking().SingleOrDefaultAsync(
            category => category.Id == id
                && category.IsActive
                && (category.IsSystem || category.UserId == userId),
            cancellationToken);

        return category ?? throw new AppException(ApplicationError.Validation, "Category is invalid.");
    }

    private async Task<CategoryBudget> GetOwnedCategoryBudgetAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var budget = await dbContext.CategoryBudgets
            .Join(
                dbContext.BudgetPeriods.Where(period => period.UserId == userId),
                budget => budget.BudgetPeriodId,
                period => period.Id,
                (budget, period) => budget)
            .SingleOrDefaultAsync(budget => budget.Id == id, cancellationToken);

        return budget ?? throw new AppException(ApplicationError.NotFound, "Category budget was not found.");
    }

    private async Task<Dictionary<string, long>> GetSpentByCategoryAsync(
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken) =>
        await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId
                && transaction.DeletedUtc == null
                && transaction.TransactionType == TransactionType.Expense
                && transaction.TransactionDate >= startDate
                && transaction.TransactionDate <= endDate)
            .GroupBy(transaction => transaction.CategoryId)
            .Select(group => new { CategoryId = group.Key, Spent = group.Sum(transaction => transaction.AmountCents) })
            .ToDictionaryAsync(row => row.CategoryId, row => row.Spent, cancellationToken);

    private static CategoryBudgetResponse ToResponse(
        CategoryBudget budget,
        BudgetCategory category,
        IReadOnlyDictionary<string, long> spentByCategory) =>
        new(
            budget.Id,
            budget.BudgetPeriodId,
            budget.CategoryId,
            category.Name,
            budget.AllocatedAmountCents,
            budget.RolloverAmountCents,
            budget.WarningThresholdPercent,
            spentByCategory.GetValueOrDefault(budget.CategoryId),
            budget.CreatedUtc,
            budget.UpdatedUtc);
}
