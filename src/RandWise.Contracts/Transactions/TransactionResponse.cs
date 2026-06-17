namespace RandWise.Contracts.Transactions;

public sealed record TransactionResponse(
    string Id,
    long AmountInCents,
    string TransactionType,
    string CategoryId,
    string Description,
    string? Merchant,
    DateOnly TransactionDate,
    string Source,
    string Status,
    string? Notes,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    DateTime? DeletedUtc);
