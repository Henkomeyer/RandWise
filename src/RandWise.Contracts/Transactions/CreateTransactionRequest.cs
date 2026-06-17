namespace RandWise.Contracts.Transactions;

public sealed record CreateTransactionRequest(
    long AmountInCents,
    string TransactionType,
    string? CategoryId,
    string Description,
    string? Merchant,
    DateOnly TransactionDate,
    string Source);
