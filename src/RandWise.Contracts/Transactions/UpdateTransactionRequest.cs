namespace RandWise.Contracts.Transactions;

public sealed record UpdateTransactionRequest(
    long AmountInCents,
    string TransactionType,
    string CategoryId,
    string Description,
    string? Merchant,
    DateOnly TransactionDate,
    string? Notes);
