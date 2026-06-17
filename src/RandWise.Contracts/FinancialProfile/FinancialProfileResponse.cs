namespace RandWise.Contracts.FinancialProfile;

public sealed record FinancialProfileResponse(
    string Id,
    long DefaultMonthlyIncomeCents,
    int? PaydayDay,
    string BudgetCycleType,
    long StartingBalanceCents,
    long SafetyBufferCents,
    long SavingsCommitmentCents,
    string NotificationMode,
    string FirstDayOfWeek,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);
