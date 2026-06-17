namespace RandWise.Contracts.Dashboard;

public sealed record SafeToSpendResponse(
    string BudgetPeriodId,
    long AvailableCashInCents,
    long ProtectedAmountInCents,
    long SafetyBufferInCents,
    long SavingsCommitmentInCents,
    long UpcomingCommitmentsInCents,
    long RemainingCategoryBudgetInCents,
    long AmountInCents,
    long DailyAmountInCents,
    int DaysRemaining);
