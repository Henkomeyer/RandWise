namespace RandWise.Contracts.Budgeting;

public sealed record BudgetPeriodResponse(
    string Id,
    DateOnly StartDate,
    DateOnly EndDate,
    long ExpectedIncomeCents,
    long ActualIncomeCents,
    long OpeningBalanceCents,
    string Status,
    int DaysRemaining,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);
