namespace RandWise.Contracts.Budgeting;

public sealed record CategoryBudgetRequest(
    string CategoryId,
    long AllocatedAmountCents,
    long RolloverAmountCents,
    int WarningThresholdPercent);
