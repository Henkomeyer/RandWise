using Microsoft.EntityFrameworkCore;
using RandWise.Application.Budgeting;
using RandWise.Application.Common;
using RandWise.Contracts.Dashboard;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Infrastructure.Budgeting;

public sealed class EfSafeToSpendService : ISafeToSpendService
{
    private readonly RandWiseDbContext dbContext;

    public EfSafeToSpendService(RandWiseDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<SafeToSpendResponse> GetCurrentAsync(
        string userId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var period = await dbContext.BudgetPeriods
            .AsNoTracking()
            .Where(period => period.UserId == userId
                && period.StartDate <= today
                && period.EndDate >= today
                && period.Status == BudgetPeriodStatus.Open)
            .OrderBy(period => period.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (period is null)
        {
            throw new AppException(ApplicationError.NotFound, "Current budget period was not found.");
        }

        var profile = await dbContext.FinancialProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        var income = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId
                && transaction.DeletedUtc == null
                && transaction.TransactionType == TransactionType.Income
                && transaction.TransactionDate >= period.StartDate
                && transaction.TransactionDate <= today)
            .SumAsync(transaction => transaction.AmountCents, cancellationToken);

        var spending = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId
                && transaction.DeletedUtc == null
                && transaction.TransactionType == TransactionType.Expense
                && transaction.TransactionDate >= period.StartDate
                && transaction.TransactionDate <= today)
            .SumAsync(transaction => transaction.AmountCents, cancellationToken);

        var remainingCategoryBudget = await GetRemainingCategoryBudgetAsync(userId, period.Id, cancellationToken);
        var upcomingCommitments = await dbContext.RecurringTransactions
            .AsNoTracking()
            .Where(recurring => recurring.UserId == userId
                && recurring.IsActive
                && recurring.TransactionType == TransactionType.Expense
                && recurring.NextOccurrenceDate >= today
                && recurring.NextOccurrenceDate <= period.EndDate)
            .SumAsync(recurring => recurring.AmountCents, cancellationToken);

        var safetyBuffer = profile?.SafetyBufferCents ?? 0;
        var savingsCommitment = profile?.SavingsCommitmentCents ?? 0;
        var availableCash = period.OpeningBalanceCents + period.ExpectedIncomeCents + income - spending;
        var protectedAmount = safetyBuffer + savingsCommitment + upcomingCommitments;
        var safeToSpend = Math.Max(0, Math.Min(availableCash - protectedAmount, remainingCategoryBudget));
        var daysRemaining = period.DaysRemaining(today);
        var dailyAmount = daysRemaining == 0 ? 0 : safeToSpend / daysRemaining;

        return new SafeToSpendResponse(
            period.Id,
            availableCash,
            protectedAmount,
            safetyBuffer,
            savingsCommitment,
            upcomingCommitments,
            remainingCategoryBudget,
            safeToSpend,
            dailyAmount,
            daysRemaining);
    }

    private async Task<long> GetRemainingCategoryBudgetAsync(
        string userId,
        string budgetPeriodId,
        CancellationToken cancellationToken)
    {
        var budgets = await dbContext.CategoryBudgets
            .AsNoTracking()
            .Where(budget => budget.BudgetPeriodId == budgetPeriodId)
            .Join(
                dbContext.BudgetPeriods.AsNoTracking().Where(period => period.UserId == userId),
                budget => budget.BudgetPeriodId,
                period => period.Id,
                (budget, period) => budget)
            .ToListAsync(cancellationToken);

        if (budgets.Count == 0)
        {
            return long.MaxValue;
        }

        var period = await dbContext.BudgetPeriods
            .AsNoTracking()
            .SingleAsync(period => period.Id == budgetPeriodId, cancellationToken);

        var spentByCategory = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId
                && transaction.DeletedUtc == null
                && transaction.TransactionType == TransactionType.Expense
                && transaction.TransactionDate >= period.StartDate
                && transaction.TransactionDate <= period.EndDate)
            .GroupBy(transaction => transaction.CategoryId)
            .Select(group => new { CategoryId = group.Key, Spent = group.Sum(transaction => transaction.AmountCents) })
            .ToDictionaryAsync(row => row.CategoryId, row => row.Spent, cancellationToken);

        return budgets.Sum(budget =>
            Math.Max(0, budget.AllocatedAmountCents + budget.RolloverAmountCents - spentByCategory.GetValueOrDefault(budget.CategoryId)));
    }
}
