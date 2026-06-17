using Microsoft.EntityFrameworkCore;
using RandWise.Application.Budgeting;
using RandWise.Application.Common;
using RandWise.Application.Dashboard;
using RandWise.Contracts.Dashboard;
using RandWise.Domain.Entities;
using RandWise.Domain.Enums;
using RandWise.Infrastructure.Persistence;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Infrastructure.Dashboard;

public sealed class EfDashboardService : IDashboardService
{
    private readonly RandWiseDbContext dbContext;
    private readonly ISafeToSpendService safeToSpendService;
    private readonly IClock clock;

    public EfDashboardService(
        RandWiseDbContext dbContext,
        ISafeToSpendService safeToSpendService,
        IClock clock)
    {
        this.dbContext = dbContext;
        this.safeToSpendService = safeToSpendService;
        this.clock = clock;
    }

    public async Task<DashboardResponse> GetAsync(
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

        var safeToSpend = await safeToSpendService.GetCurrentAsync(userId, today, cancellationToken);
        var periodProgressPercent = GetPercent(
            Math.Clamp(today.DayNumber - period.StartDate.DayNumber + 1, 0, period.EndDate.DayNumber - period.StartDate.DayNumber + 1),
            period.EndDate.DayNumber - period.StartDate.DayNumber + 1);

        var expenses = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId
                && transaction.DeletedUtc == null
                && transaction.TransactionType == TransactionType.Expense
                && transaction.TransactionDate >= period.StartDate
                && transaction.TransactionDate <= today)
            .ToListAsync(cancellationToken);

        var spentThisPeriod = expenses.Sum(transaction => transaction.AmountCents);
        var categories = await BuildCategoryProgressAsync(userId, period, expenses, cancellationToken);
        var upcomingCommitments = await BuildUpcomingCommitmentsAsync(userId, today, period.EndDate, cancellationToken);
        var recentTransactions = await BuildRecentTransactionsAsync(userId, cancellationToken);
        var spendingBudget = categories.Sum(category => category.AllocatedInCents);
        if (spendingBudget <= 0)
        {
            spendingBudget = Math.Max(0, period.ExpectedIncomeCents + period.OpeningBalanceCents - safeToSpend.ProtectedAmountInCents);
        }

        var spendingPercent = GetPercent(spentThisPeriod, spendingBudget);
        var moneyPulse = CalculateMoneyPulse(
            safeToSpend.AmountInCents,
            spendingPercent,
            periodProgressPercent,
            categories);
        var status = GetFinancialStatus(moneyPulse);
        var recommendation = GetRecommendedAction(
            safeToSpend,
            spendingPercent,
            periodProgressPercent,
            categories,
            upcomingCommitments);
        var cashFlow = BuildCashFlowForecast(today, period.EndDate, safeToSpend.AvailableCashInCents, upcomingCommitments);
        var insights = BuildInsights(safeToSpend, spendingPercent, periodProgressPercent, categories, upcomingCommitments);

        return new DashboardResponse(
            clock.UtcNow,
            new DashboardBudgetPeriodResponse(
                period.Id,
                period.StartDate,
                period.EndDate,
                period.DaysRemaining(today),
                periodProgressPercent),
            new DashboardFinancialStatusResponse(status, GetStatusMessage(status), moneyPulse),
            new DashboardSafeToSpendSummaryResponse(
                safeToSpend.AmountInCents,
                safeToSpend.DailyAmountInCents,
                safeToSpend.AvailableCashInCents,
                safeToSpend.ProtectedAmountInCents,
                safeToSpend.SafetyBufferInCents,
                safeToSpend.SavingsCommitmentInCents,
                safeToSpend.UpcomingCommitmentsInCents,
                safeToSpend.RemainingCategoryBudgetInCents),
            new DashboardSpendingSummaryResponse(spentThisPeriod, spendingPercent, periodProgressPercent),
            recommendation,
            categories,
            upcomingCommitments,
            recentTransactions,
            cashFlow,
            insights);
    }

    private async Task<IReadOnlyList<DashboardCategoryProgressResponse>> BuildCategoryProgressAsync(
        string userId,
        BudgetPeriod period,
        IReadOnlyList<Transaction> expenses,
        CancellationToken cancellationToken)
    {
        var latestByCategory = expenses
            .GroupBy(transaction => transaction.CategoryId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(transaction => transaction.TransactionDate)
                    .ThenByDescending(transaction => transaction.CreatedUtc)
                    .First().Description);

        var spentByCategory = expenses
            .GroupBy(transaction => transaction.CategoryId)
            .ToDictionary(group => group.Key, group => group.Sum(transaction => transaction.AmountCents));

        var rows = await dbContext.CategoryBudgets
            .AsNoTracking()
            .Where(budget => budget.BudgetPeriodId == period.Id)
            .Join(
                dbContext.BudgetCategories.AsNoTracking().Where(category =>
                    category.IsActive && (category.IsSystem || category.UserId == userId)),
                budget => budget.CategoryId,
                category => category.Id,
                (budget, category) => new { budget, category })
            .OrderBy(row => row.category.SortOrder)
            .ThenBy(row => row.category.Name)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new DashboardCategoryProgressResponse(
                row.category.Id,
                row.category.Name,
                row.budget.AllocatedAmountCents + row.budget.RolloverAmountCents,
                spentByCategory.GetValueOrDefault(row.category.Id),
                Math.Max(
                    0,
                    row.budget.AllocatedAmountCents
                    + row.budget.RolloverAmountCents
                    - spentByCategory.GetValueOrDefault(row.category.Id)),
                GetPercent(spentByCategory.GetValueOrDefault(row.category.Id), row.budget.AllocatedAmountCents + row.budget.RolloverAmountCents),
                GetCategoryStatus(
                    GetPercent(spentByCategory.GetValueOrDefault(row.category.Id), row.budget.AllocatedAmountCents + row.budget.RolloverAmountCents),
                    row.budget.WarningThresholdPercent),
                latestByCategory.GetValueOrDefault(row.category.Id)))
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardUpcomingCommitmentResponse>> BuildUpcomingCommitmentsAsync(
        string userId,
        DateOnly today,
        DateOnly endDate,
        CancellationToken cancellationToken) =>
        await dbContext.RecurringTransactions
            .AsNoTracking()
            .Where(recurring => recurring.UserId == userId
                && recurring.IsActive
                && recurring.TransactionType == TransactionType.Expense
                && recurring.NextOccurrenceDate >= today
                && recurring.NextOccurrenceDate <= endDate)
            .OrderBy(recurring => recurring.NextOccurrenceDate)
            .Take(6)
            .Select(recurring => new DashboardUpcomingCommitmentResponse(
                recurring.Id,
                recurring.Description,
                recurring.NextOccurrenceDate,
                recurring.AmountCents,
                true,
                "protected"))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<DashboardRecentTransactionResponse>> BuildRecentTransactionsAsync(
        string userId,
        CancellationToken cancellationToken) =>
        await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId && transaction.DeletedUtc == null)
            .Join(
                dbContext.BudgetCategories.AsNoTracking(),
                transaction => transaction.CategoryId,
                category => category.Id,
                (transaction, category) => new { transaction, category })
            .OrderByDescending(row => row.transaction.TransactionDate)
            .ThenByDescending(row => row.transaction.CreatedUtc)
            .Take(6)
            .Select(row => new DashboardRecentTransactionResponse(
                row.transaction.Id,
                row.transaction.Description,
                row.transaction.Merchant,
                row.category.Name,
                row.transaction.TransactionDate,
                row.transaction.AmountCents,
                row.transaction.TransactionType.ToContract(),
                row.transaction.Source.ToContract(),
                row.transaction.Status.ToContract()))
            .ToListAsync(cancellationToken);

    private static IReadOnlyList<DashboardCashFlowPointResponse> BuildCashFlowForecast(
        DateOnly today,
        DateOnly endDate,
        long availableCash,
        IReadOnlyList<DashboardUpcomingCommitmentResponse> upcomingCommitments)
    {
        var points = new List<DashboardCashFlowPointResponse>();
        var balance = availableCash;
        var horizon = Math.Min(14, Math.Max(1, endDate.DayNumber - today.DayNumber + 1));
        var commitmentsByDate = upcomingCommitments
            .GroupBy(commitment => commitment.DueDate)
            .ToDictionary(group => group.Key, group => group.Sum(commitment => commitment.AmountInCents));

        for (var index = 0; index < horizon; index++)
        {
            var date = today.AddDays(index);
            var commitmentAmount = commitmentsByDate.GetValueOrDefault(date);
            balance -= commitmentAmount;
            points.Add(new DashboardCashFlowPointResponse(date, balance, commitmentAmount, date == endDate));
        }

        return points;
    }

    private static IReadOnlyList<DashboardInsightResponse> BuildInsights(
        SafeToSpendResponse safeToSpend,
        int spendingPercent,
        int periodProgressPercent,
        IReadOnlyList<DashboardCategoryProgressResponse> categories,
        IReadOnlyList<DashboardUpcomingCommitmentResponse> upcomingCommitments)
    {
        var insights = new List<DashboardInsightResponse>();
        var riskiestCategory = categories
            .OrderByDescending(category => category.SpendingPercent)
            .FirstOrDefault();

        if (safeToSpend.AmountInCents <= 0)
        {
            insights.Add(new DashboardInsightResponse(
                "safeToSpend",
                "Safe-to-spend is fully protected",
                "All available cash is currently assigned to commitments, savings or the safety buffer."));
        }

        if (spendingPercent > periodProgressPercent + 10)
        {
            insights.Add(new DashboardInsightResponse(
                "spendingPace",
                "Spending is ahead of the calendar",
                "Your used budget percentage is higher than the period progress."));
        }

        if (riskiestCategory is not null && riskiestCategory.SpendingPercent >= 80)
        {
            insights.Add(new DashboardInsightResponse(
                "categoryRisk",
                $"{riskiestCategory.Name} needs attention",
                "This category is close to or above its warning threshold."));
        }

        if (upcomingCommitments.Count > 0)
        {
            insights.Add(new DashboardInsightResponse(
                "commitments",
                "Upcoming commitments are protected",
                $"{upcomingCommitments.Count} fixed cost entries are reserved before flexible spending."));
        }

        if (insights.Count == 0)
        {
            insights.Add(new DashboardInsightResponse(
                "positive",
                "Budget is stable",
                "Safe-to-spend, commitments and category pacing are currently in a workable range."));
        }

        return insights.Take(3).ToList();
    }

    private static DashboardRecommendedActionResponse GetRecommendedAction(
        SafeToSpendResponse safeToSpend,
        int spendingPercent,
        int periodProgressPercent,
        IReadOnlyList<DashboardCategoryProgressResponse> categories,
        IReadOnlyList<DashboardUpcomingCommitmentResponse> upcomingCommitments)
    {
        if (safeToSpend.AmountInCents <= 0)
        {
            return new DashboardRecommendedActionResponse(
                "negativeSafeToSpend",
                "Protect your essentials",
                "Avoid flexible spending until income, savings or commitments change.");
        }

        var overrun = categories
            .Where(category => category.SpendingPercent >= 100)
            .OrderByDescending(category => category.SpendingPercent)
            .FirstOrDefault();
        if (overrun is not null)
        {
            return new DashboardRecommendedActionResponse(
                "categoryOverrun",
                $"Pause {overrun.Name} spending",
                "This category has reached its planned amount for the period.");
        }

        if (spendingPercent > periodProgressPercent + 10)
        {
            return new DashboardRecommendedActionResponse(
                "spendingPace",
                "Slow down flexible spending",
                "Spending is running ahead of the budget-period timeline.");
        }

        var nextCommitment = upcomingCommitments.FirstOrDefault();
        if (nextCommitment is not null && nextCommitment.AmountInCents > safeToSpend.AmountInCents)
        {
            return new DashboardRecommendedActionResponse(
                "upcomingCommitment",
                $"Prepare for {nextCommitment.Description}",
                "The next commitment is larger than the current safe-to-spend amount.");
        }

        return new DashboardRecommendedActionResponse(
            "onTrack",
            "Keep the current pace",
            "Safe-to-spend is positive and spending is aligned with this budget period.");
    }

    private static int CalculateMoneyPulse(
        long safeToSpend,
        int spendingPercent,
        int periodProgressPercent,
        IReadOnlyList<DashboardCategoryProgressResponse> categories)
    {
        var score = 100;
        if (safeToSpend <= 0)
        {
            score -= 35;
        }

        score -= Math.Max(0, spendingPercent - periodProgressPercent) / 2;
        score -= categories.Count(category => category.SpendingPercent >= 100) * 12;
        score -= categories.Count(category => category.SpendingPercent is >= 80 and < 100) * 6;

        return Math.Clamp(score, 0, 100);
    }

    private static string GetFinancialStatus(int moneyPulse) =>
        moneyPulse switch
        {
            >= 85 => "comfortable",
            >= 70 => "onTrack",
            >= 50 => "watchSpending",
            >= 30 => "budgetPressure",
            _ => "immediateAttention"
        };

    private static string GetStatusMessage(string status) =>
        status switch
        {
            "comfortable" => "You have room in the plan.",
            "onTrack" => "You are currently on track.",
            "watchSpending" => "Watch flexible spending for the rest of this period.",
            "budgetPressure" => "Budget pressure is building.",
            _ => "Immediate attention is needed."
        };

    private static string GetCategoryStatus(int spendingPercent, int warningThresholdPercent)
    {
        if (spendingPercent >= 100)
        {
            return "over";
        }

        if (spendingPercent >= warningThresholdPercent)
        {
            return "warning";
        }

        return "onTrack";
    }

    private static int GetPercent(long value, long total)
    {
        if (total <= 0)
        {
            return 0;
        }

        return (int)Math.Clamp(Math.Round(value * 100m / total), 0, 999);
    }
}
