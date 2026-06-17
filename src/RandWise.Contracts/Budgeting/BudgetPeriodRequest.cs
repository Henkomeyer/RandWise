namespace RandWise.Contracts.Budgeting;

public sealed record BudgetPeriodRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    long ExpectedIncomeCents,
    long OpeningBalanceCents);
