namespace RandWise.Contracts.Transactions;

public sealed record CategoriseTransactionRequest(
    string CategoryId,
    bool CreateRule = false,
    string? MatchType = null,
    string? MatchValue = null);
