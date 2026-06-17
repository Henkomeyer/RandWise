namespace RandWise.Contracts.FinancialProfile;

public sealed record FinancialProfileRequest(
    long DefaultMonthlyIncomeCents,
    int? PaydayDay,
    string BudgetCycleType,
    long StartingBalanceCents,
    long SafetyBufferCents,
    long SavingsCommitmentCents,
    string NotificationMode,
    string FirstDayOfWeek);
