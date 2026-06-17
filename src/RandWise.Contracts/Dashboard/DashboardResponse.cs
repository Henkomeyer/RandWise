namespace RandWise.Contracts.Dashboard;

public sealed record DashboardResponse(
    DateTime GeneratedUtc,
    DashboardBudgetPeriodResponse BudgetPeriod,
    DashboardFinancialStatusResponse FinancialStatus,
    DashboardSafeToSpendSummaryResponse SafeToSpend,
    DashboardSpendingSummaryResponse Spending,
    DashboardRecommendedActionResponse RecommendedAction,
    IReadOnlyList<DashboardCategoryProgressResponse> Categories,
    IReadOnlyList<DashboardUpcomingCommitmentResponse> UpcomingCommitments,
    IReadOnlyList<DashboardRecentTransactionResponse> RecentTransactions,
    IReadOnlyList<DashboardCashFlowPointResponse> CashFlowForecast,
    IReadOnlyList<DashboardInsightResponse> Insights);

public sealed record DashboardBudgetPeriodResponse(
    string Id,
    DateOnly StartDate,
    DateOnly EndDate,
    int DaysRemaining,
    int PeriodProgressPercent);

public sealed record DashboardFinancialStatusResponse(
    string Status,
    string Message,
    int MoneyPulse);

public sealed record DashboardSafeToSpendSummaryResponse(
    long AmountInCents,
    long DailyAmountInCents,
    long AvailableCashInCents,
    long ProtectedAmountInCents,
    long SafetyBufferInCents);

public sealed record DashboardSpendingSummaryResponse(
    long SpentThisPeriodInCents,
    int SpendingPercent,
    int ExpectedSpendingPercent);

public sealed record DashboardRecommendedActionResponse(
    string Type,
    string Title,
    string Message);

public sealed record DashboardCategoryProgressResponse(
    string CategoryId,
    string Name,
    long AllocatedInCents,
    long SpentInCents,
    long RemainingInCents,
    int SpendingPercent,
    string Status,
    string? LatestTransaction);

public sealed record DashboardUpcomingCommitmentResponse(
    string Id,
    string Description,
    DateOnly DueDate,
    long AmountInCents,
    bool IsProtected,
    string Status);

public sealed record DashboardRecentTransactionResponse(
    string Id,
    string Description,
    string? Merchant,
    string CategoryName,
    DateOnly TransactionDate,
    long AmountInCents,
    string TransactionType,
    string Source,
    string Status);

public sealed record DashboardCashFlowPointResponse(
    DateOnly Date,
    long ProjectedBalanceInCents,
    long CommitmentAmountInCents,
    bool IsPayday);

public sealed record DashboardInsightResponse(
    string Type,
    string Title,
    string Message);
