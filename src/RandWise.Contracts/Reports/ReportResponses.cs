namespace RandWise.Contracts.Reports;

public sealed record WeeklyFinancialStoryResponse(
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    long IncomeInCents,
    long ExpenseInCents,
    long NetInCents,
    int TransactionCount,
    string Summary,
    IReadOnlyList<ReportCategoryTotalResponse> TopCategories);

public sealed record MonthlyMoneyWrapResponse(
    int Year,
    int Month,
    long IncomeInCents,
    long ExpenseInCents,
    long NetInCents,
    long AverageDailyExpenseInCents,
    string Summary,
    IReadOnlyList<ReportCategoryTotalResponse> TopCategories);

public sealed record CategoryBreakdownResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<ReportCategoryTotalResponse> Categories);

public sealed record ReportCategoryTotalResponse(
    string CategoryId,
    string CategoryName,
    long AmountInCents,
    int TransactionCount);
