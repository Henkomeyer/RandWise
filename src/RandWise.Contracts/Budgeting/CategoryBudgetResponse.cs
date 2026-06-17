namespace RandWise.Contracts.Budgeting;

public sealed record CategoryBudgetResponse(
    string Id,
    string BudgetPeriodId,
    string CategoryId,
    string CategoryName,
    long AllocatedAmountCents,
    long RolloverAmountCents,
    int WarningThresholdPercent,
    long SpentAmountCents,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);
