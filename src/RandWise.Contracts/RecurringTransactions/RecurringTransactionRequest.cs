namespace RandWise.Contracts.RecurringTransactions;

public sealed record RecurringTransactionRequest(
    string CategoryId,
    string Description,
    string? Merchant,
    long AmountInCents,
    string TransactionType,
    string Frequency,
    int? DayOfMonth,
    string? DayOfWeek,
    DateOnly NextOccurrenceDate,
    DateOnly? EndDate,
    bool AutoCreate);
